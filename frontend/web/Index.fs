namespace CompleteInformation.Core.Frontend.Web

open Elmish
open Fable.Remoting.Client

open CompleteInformation.Core.Api

module Index =
    type Model = { text: string }

    type Msg =
        | GetPlugins
        | SetText of string

    let pluginApi =
        Remoting.createApi ()
        |> Remoting.withBaseUrl "http://localhost:8084/api"
        |> Remoting.buildProxy<PluginApi>

    let init () : Model * Cmd<Msg> =
        let model = { text = "" }

        let cmd = Cmd.none

        model, cmd

    let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
        match msg with
        | GetPlugins ->
            let cmd = Cmd.OfAsync.perform pluginApi.get () (String.concat "," >> SetText)

            model, cmd
        | SetText text -> { model with text = text }, Cmd.none

    open Feliz
    open Feliz.Bulma

    let navBrand =
        Bulma.navbarBrand.div [
            Bulma.navbarItem.a [
                prop.href "https://safe-stack.github.io/"
                navbarItem.isActive
                prop.children [
                    Html.img [
                        prop.src "/favicon.png"
                        prop.alt "Logo"
                    ]
                ]
            ]
        ]

    let containerBox (model: Model) (dispatch: Msg -> unit) =
        Bulma.box [
            Bulma.field.div [
                field.isGrouped
                prop.children [
                    Bulma.button.button [
                        prop.text "Fetch"
                        prop.onClick (fun _ -> GetPlugins |> dispatch)
                    ]
                    Bulma.text.p [ prop.text model.text ]
                ]
            ]
        ]

    let view (model: Model) (dispatch: Msg -> unit) =
        Bulma.hero [
            hero.isFullHeight
            color.isPrimary
            prop.style [
                style.backgroundSize "cover"
                style.backgroundImageUrl "https://unsplash.it/1200/900?random"
                style.backgroundPosition "no-repeat center center fixed"
            ]
            prop.children [
                Bulma.heroHead [
                    Bulma.navbar [
                        Bulma.container [ navBrand ]
                    ]
                ]
                Bulma.heroBody [
                    Bulma.container [
                        Bulma.column [
                            column.is6
                            column.isOffset3
                            prop.children [
                                Bulma.title [
                                    text.hasTextCentered
                                    prop.text "SAFE.App"
                                ]
                                containerBox model dispatch
                            ]
                        ]
                    ]
                ]
            ]
        ]
