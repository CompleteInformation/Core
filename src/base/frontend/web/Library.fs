namespace CompleteInformation.Base.Frontend.Web

open Elmish
open Elmish.React
open Fable.Remoting.Client

open CompleteInformation.Core

[<RequireQualifiedAccess>]
module Api =
    let createBase () =
        Remoting.createApi () |> Remoting.withBaseUrl "http://localhost:8084/api"

[<RequireQualifiedAccess>]
module Program =
    let mount program =
        Program.withReactSynchronous Constant.moduleSlotId program
