namespace CompleteInformation.Core.Frontend.Web

open Elmish
open Fable.Remoting.Client

open CompleteInformation.Core.Api
open CompleteInformation.Core.PluginBase

module Index =
    type Model = { plugins: PluginMetadata list }

    type Msg =
        | GetPlugins
        | SetPlugins of PluginMetadata list

    let pluginApi =
        Remoting.createApi ()
        |> Remoting.withBaseUrl "http://localhost:8084/api"
        |> Remoting.buildProxy<PluginApi>

    let init () : Model * Cmd<Msg> =
        let model = { plugins = [] }

        let cmd = Cmd.ofMsg GetPlugins

        model, cmd

    let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
        match msg with
        | GetPlugins ->
            let cmd = Cmd.OfAsync.perform pluginApi.get () SetPlugins

            model, cmd
        | SetPlugins plugins -> { model with plugins = plugins }, Cmd.none

    open Feliz
    open Feliz.Bulma

    let navItems plugins =
        [
            for plugin: PluginMetadata in plugins do
                let pluginId = PluginId.unwrap plugin.id

                Bulma.navbarItem.a [
                    prop.href $"/plugin/{pluginId}/index.html"
                    prop.text plugin.name
                ]
        ]

    let navBar plugins =
        Bulma.navbar [
            Bulma.navbarMenu [
                Bulma.navbarStart.div [
                    for item in navItems plugins do
                        item
                ]
            ]
        ]

    let content plugins =
        [
            Bulma.title "Complete Information"
            Bulma.block [
                prop.text "Welcome to Complete Information, your central place for managing your data."
            ]
            // Give a hint if there are no plugins
            if Seq.isEmpty plugins then
                Bulma.block [
                    prop.text
                        "It looks like you have no plugins installed. Download some and put them in the plugins and WebRoot/plugins folder."
                ]
        ]

    let view (model: Model) (dispatch: Msg -> unit) =
        Html.div [
            navBar model.plugins
            Bulma.container (content model.plugins)
        ]
