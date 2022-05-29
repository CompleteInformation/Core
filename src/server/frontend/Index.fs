namespace CompleteInformation.Core.Frontend.Web

open Elmish
open Fable.Core
open Fable.Remoting.Client

open CompleteInformation.Server.Api
open CompleteInformation.Shared.Core

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
    type Model =
        {
            /// List of installed plugins, if None they are still loading
            plugins: PluginMetadata list option
        }

    type Msg =
        | GetPlugins
        | OnPluginsLoaded of PluginMetadata list
        | ActivatePlugin of PluginId

    let pluginApi =
        Remoting.createApi ()
        |> Remoting.withBaseUrl "http://localhost:8084/api"
        |> Remoting.buildProxy<PluginApi>

    let init () : Model * Cmd<Msg> =
        let model = { plugins = None }

        let cmd = Cmd.ofMsg GetPlugins

        model, cmd

    let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
        match msg with
        | GetPlugins ->
            let cmd = Cmd.OfAsync.perform pluginApi.get () OnPluginsLoaded

            model, cmd
        | OnPluginsLoaded plugins -> { model with plugins = Some plugins }, Cmd.none
        | ActivatePlugin id ->
            activatePlugin id

            model, Cmd.none

    open Feliz
    open Feliz.Bulma

    let navItems dispatch plugins =
        [
            for plugin in plugins do
                Bulma.navbarItem.div [
                    prop.text plugin.name
                    prop.onClick (fun _ -> ActivatePlugin plugin.id |> dispatch)
                ]
        ]

    let navBar dispatch plugins =
        Bulma.navbar [
            Bulma.navbarMenu [
                Bulma.navbarStart.div (navItems dispatch plugins)
            ]
        ]

    let content plugins =
        [
            Bulma.title "Complete Information"
            Bulma.block [
                prop.text "Welcome to Complete Information, your central place for managing your data."
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

    let view (model: Model) (dispatch: Msg -> unit) =
        Html.div [
            navBar dispatch (Option.defaultValue [] model.plugins)
            Bulma.container [
                prop.id "main-container"
                prop.children (content model.plugins)
            ]
        ]
