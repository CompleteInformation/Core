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
        | SetUser of User option
        | SetUserList of User list option
        | SelectUser of User
        | ChangeCreateUserName of string
        | CreateUser
        | OnPluginsLoaded of PluginMetadata list
        | ActivatePlugin of PluginId

    let apiBaseUrl = "http://localhost:8084/api"

    let inline createApi<'a> () =
        Remoting.createApi ()
        |> Remoting.withBaseUrl apiBaseUrl
        |> Remoting.buildProxy<'a>

    let pluginApi = createApi<PluginApi> ()

    let userApi = createApi<UserApi> ()

    let init () : Model * Cmd<Msg> =
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
            let cmd = Cmd.OfAsync.perform userApi.get userId SetUser

            model, cmd
        | FetchUserList ->
            let cmd =
                Cmd.OfAsync.perform userApi.getList () (fun userList -> Some userList |> SetUserList)

            model, cmd
        | SetUser user -> { model with user = user }, Cmd.none
        | SetUserList userList -> { model with userList = userList }, Cmd.none
        | SelectUser user ->
            // We only write this once here, that's why this isn't in a library
            localStorage.setItem (Constant.userIdKey, UserId.unwrap user.id |> string)

            { model with user = Some user }, Cmd.none
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

    let content plugins (user: User) = [
        Bulma.title "Complete Information"
        Bulma.block [
            prop.text "Welcome to Complete Information, your central place for managing your data."
        ]
        Bulma.block [
            prop.text $"You are currently logged in as {user.name} (ID: {UserId.unwrap user.id})."
        ]
        // Give a hint if there are no plugins
        match plugins with
        | Some plugins when Seq.isEmpty plugins ->
            Bulma.block [
                prop.text
                    "It looks like you have no plugins installed. Download some and put them in the plugins and WebRoot/plugins folder."
            ]
        | _ -> ()
    ]

    let userView (model: Model) (dispatch: Msg -> unit) =
        Bulma.container [
            Bulma.columns [
                Bulma.column [
                    Bulma.block [ prop.text "No User selected!" ]

                    // Create a new user
                    Html.input [
                        prop.placeholder "Name"
                        prop.onTextChange (fun text -> ChangeCreateUserName text |> dispatch)
                    ]
                    Html.button [ prop.text "Create"; prop.onClick (fun _ -> CreateUser |> dispatch) ]

                    // Select a user
                    match model.userList with
                    | None -> Html.div [ prop.text "Loading users..." ]
                    | Some [] -> () // No users yet
                    | Some userList ->
                        Bulma.title "Select user"
                        Bulma.block [ prop.text "Please select your user." ]

                        for user in userList do
                            Html.button [ prop.text user.name; prop.onClick (fun _ -> SelectUser user |> dispatch) ]
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
                Bulma.container [ prop.id "main-container"; prop.children (content model.plugins user) ]
            ]
