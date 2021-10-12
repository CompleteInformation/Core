namespace CompleteInformation.Core.Frontend.Web

open Elmish
open Fable.Remoting.Client

open CompleteInformation.Core.Api

module Index =
    type Model = { text: string }

    type Msg =
        | GetUser of UserId
        | SetText of string

    let todosApi =
        Remoting.createApi ()
        |> Remoting.withBaseUrl "http://localhost:8081"
        |> Remoting.buildProxy<IUserApi>

    let init () : Model * Cmd<Msg> =
        let model = { text = "start" }

        let cmd = Cmd.none

        model, cmd

    let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
        match msg with
        | GetUser userId ->
            let cmd =
                Cmd.OfAsync.perform
                    todosApi.get
                    userId
                    (function
                     | Some user -> user.name
                     | None -> "error"
                     >> SetText)

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
                    Bulma.text.p [ prop.text model.text ]
                    Bulma.button.button [
                        prop.text "Fetch"
                        prop.onClick (fun _ -> GetUser(UserId 1u) |> dispatch)
                    ]
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
