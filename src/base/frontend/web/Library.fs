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
        localStorage.getItem Constant.userIdKey
        |> function
            | null -> None
            | id -> Guid.Parse id |> UserId |> Some

[<RequireQualifiedAccess>]
module Api =
    let createBase () =
        Remoting.createApi () |> Remoting.withBaseUrl "http://localhost:8084/api"

[<RequireQualifiedAccess>]
module Program =
    let mount program =
        Program.withReactSynchronous Constant.moduleSlotId program
