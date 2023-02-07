namespace CompleteInformation.Base.Frontend.Web

open Browser
open Elmish
open Elmish.React
open Fable.Remoting.Client
open System

open CompleteInformation.Core

[<RequireQualifiedAccess>]
module LocalStorage =
    let getUserId () =
        localStorage.getItem Constant.LocalStorage.userIdKey
        |> function
            | null -> None
            | id -> Guid.Parse id |> UserId |> Some

    let getApiBaseUrl () =
        localStorage.getItem Constant.LocalStorage.apiBaseUrlKey
        |> function
            | null -> None
            | url -> Some url

[<RequireQualifiedAccess>]
module Api =
    let createBase () =
        let apiBase =
            LocalStorage.getApiBaseUrl ()
            |> Option.defaultWith (fun () -> $"{window.location.protocol}//{window.location.host}/api")

        Remoting.createApi () |> Remoting.withBaseUrl apiBase

[<RequireQualifiedAccess>]
module Program =
    let mount program =
        Program.withReactSynchronous Constant.moduleSlotId program
