namespace CompleteInformation.Core.Frontend.Web

open Browser
open Elmish
open Fable.Core
open Fable.Remoting.Client
open System

open CompleteInformation.Core
open CompleteInformation.Base.Frontend.Web
open CompleteInformation.Server.Api

module Native =
    [<Global>]
    let loadPlugin: (string -> unit) = jsNative

    [<Global>]
    let activatePlugin: (string -> unit) = jsNative

[<AutoOpen>]
module NativeWrapper =
    let loadPlugin = PluginId.unwrap >> Native.loadPlugin
    let activatePlugin = PluginId.unwrap >> Native.activatePlugin

module Index =
    type Model = {
        /// List of installed plugins, if None they are still loading
        plugins: PluginMetadata list option
        user: User option
        /// None if not tried to load yet
        userList: User list option
        createUser: string
    }

    type Msg =
        | GetPlugins
        | FetchUser of UserId
        | FetchUserList
        | SetUserList of User list option
        | DeselectUser
        | SelectUser of User
        | ChangeCreateUserName of string
        | CreateUser
        | OnPluginsLoaded of PluginMetadata list
        | ActivatePlugin of PluginId

    let inline createApi<'a> () =
        Api.createBase () |> Remoting.buildProxy<'a>

    let pluginApi = createApi<PluginApi> ()

    let userApi = createApi<UserApi> ()

    let init () : Model * Cmd<Msg> =
        // Set api base url
#if DEVSERVER
        let apiBaseUrl = "http://localhost:8084/api"
#else
        let apiBaseUrl = $"{window.location.host}/api"
#endif
        localStorage.setItem (Constant.LocalStorage.apiBaseUrlKey, apiBaseUrl)

        let userId = LocalStorage.getUserId ()

        let model = {
            plugins = None
            user = None
            userList = None
            createUser = ""
        }

        let msgList = [
            GetPlugins
            match userId with
            | Some userId -> FetchUser userId
            | None -> FetchUserList
        ]

        let cmd = msgList |> List.map Cmd.ofMsg |> Cmd.batch

        model, cmd

    let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
        match msg with
        | GetPlugins ->
            let cmd = Cmd.OfAsync.perform pluginApi.get () OnPluginsLoaded

            model, cmd
        | FetchUser userId ->
            let ofSuccess user =
                match user with
                | Some user -> SelectUser user
                | None ->
                    // If the user doesn't exist, we delete the ID from the local storage
                    localStorage.removeItem Constant.LocalStorage.userIdKey
                    // We then fetch the user list to allow selecting an existing one
                    FetchUserList

            let cmd = Cmd.OfAsync.perform userApi.get userId ofSuccess

            model, cmd
        | FetchUserList ->
            let cmd =
                Cmd.OfAsync.perform userApi.getList () (fun userList -> Some userList |> SetUserList)

            model, cmd
        | SetUserList userList -> { model with userList = userList }, Cmd.none
        | SelectUser user ->
            // We only write this once here, that's why this isn't in a library
            localStorage.setItem (Constant.LocalStorage.userIdKey, UserId.toString user.id)

            // Reset userlist, this isn't accurate anymore
            { model with
                user = Some user
                userList = None
            },
            Cmd.none
        | DeselectUser ->
            localStorage.removeItem Constant.LocalStorage.userIdKey

            // Fetch user list, if we don't have one yet
            let cmd =
                match model.userList with
                | None -> Cmd.ofMsg FetchUserList
                | Some _ -> Cmd.none

            { model with user = None }, cmd
        | ChangeCreateUserName name -> { model with createUser = name }, Cmd.none
        | CreateUser ->
            let cmd = Cmd.OfAsync.perform userApi.create model.createUser FetchUser

            model, cmd
        | OnPluginsLoaded plugins -> { model with plugins = Some plugins }, Cmd.none
        | ActivatePlugin id ->
            activatePlugin id

            model, Cmd.none

    open Feliz
    open Feliz.Bulma

    let navItems dispatch plugins = [
        for (plugin: PluginMetadata) in plugins do
            Bulma.navbarItem.div [
                prop.text plugin.name
                prop.onClick (fun _ -> ActivatePlugin plugin.id |> dispatch)
            ]
    ]

    let navBar dispatch plugins =
        Bulma.navbar [ Bulma.navbarMenu [ Bulma.navbarStart.div (navItems dispatch plugins) ] ]

    let content plugins (user: User) dispatch = [
        Bulma.title [ Bulma.title.is2; prop.text "Complete Information" ]
        Bulma.block [
            prop.text "Welcome to Complete Information, your central place for managing your data."
        ]
        Bulma.block [
            prop.text $"You are currently logged in as {user.name} (ID: {UserId.toString user.id})."
        ]

        Bulma.buttons [
            Bulma.button.button [
                Bulma.color.isPrimary
                Bulma.color.isLight
                prop.text "Change User"
                prop.onClick (fun _ -> DeselectUser |> dispatch)
            ]
        ]

        // Give a hint if there are no plugins
        match plugins with
        | Some plugins when Seq.isEmpty plugins ->
            let warning =
                "It looks like you have no plugins installed. Download some and put them in the plugins and WebRoot/plugins folder."

            Bulma.notification [
                Bulma.color.isWarning
                prop.children[Bulma.iconText [
                                  Bulma.icon [ Html.i [ prop.className "fas fa-exclamation-triangle" ] ]
                                  Html.span warning
                              ]]
            ]
        | _ -> ()
    ]

    let userView (model: Model) (dispatch: Msg -> unit) =
        Bulma.container [
            Bulma.title [ Bulma.title.is2; prop.text "Complete Information" ]

            Bulma.columns [
                Bulma.column [
                    Bulma.block [ prop.text "No User selected!" ]

                    // Create a new user
                    Bulma.field.div [
                        Bulma.control.div [
                            Bulma.input.text [
                                prop.placeholder "Name"
                                prop.onTextChange (fun text -> ChangeCreateUserName text |> dispatch)
                            ]
                        ]
                    ]
                    Bulma.buttons [
                        Bulma.button.button [
                            Bulma.color.isPrimary
                            Bulma.color.isLight
                            prop.text "Create"
                            prop.onClick (fun _ -> CreateUser |> dispatch)
                        ]
                    ]

                    // Select a user
                    match model.userList with
                    | None -> Html.div [ prop.text "Loading users..." ]
                    | Some [] -> () // No users yet
                    | Some userList ->
                        Bulma.title [ Bulma.title.is4; prop.text "Select user" ]
                        Bulma.block [ prop.text "Please select your user." ]

                        Bulma.buttons [
                            for user in userList do
                                Bulma.button.button [
                                    prop.text user.name
                                    prop.onClick (fun _ -> SelectUser user |> dispatch)
                                ]
                        ]
                ]
            ]
        ]

    let view (model: Model) (dispatch: Msg -> unit) =
        match model.user with
        | None ->
            // Visitor has to select its user first
            userView model dispatch
        | Some user ->
            Html.div [
                navBar dispatch (Option.defaultValue [] model.plugins)
                Bulma.container [
                    prop.id "main-container"
                    prop.children (content model.plugins user dispatch)
                ]
            ]
