namespace CompleteInformation.Base.Backend.Web

open CompleteInformation.Core

[<RequireQualifiedAccess>]
module Api =
    open Fable.Remoting.Giraffe
    open Fable.Remoting.Server

    let build api routeBuilder =
        Remoting.createApi ()
        |> Remoting.withRouteBuilder routeBuilder
        |> Remoting.fromValue api
        |> Remoting.buildHttpHandler

open Giraffe.Core
open Microsoft.AspNetCore.Http

type WebserverPlugin =
    abstract member getApi: (string -> string -> string) -> (HttpFunc -> HttpContext -> HttpFuncResult)

    abstract member getMetaData: unit -> PluginMetadata
