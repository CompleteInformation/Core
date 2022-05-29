namespace CompleteInformation.Base.Frontend.Web

open Elmish
open Elmish.React
open Fable.Remoting.Client

[<RequireQualifiedAccess>]
module Api =
    let buildProxy<'Api> () =
        Remoting.createApi ()
        |> Remoting.withBaseUrl "http://localhost:8084/api"
        |> Remoting.buildProxy<'Api>

[<RequireQualifiedAccess>]
module Program =
    let mount program =
        Program.withReactSynchronous "module-slot" program
