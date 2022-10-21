namespace CompleteInformation.Base.Backend.Web

open Fable.Remoting.Giraffe
open Fable.Remoting.Server
open Microsoft.AspNetCore.Http
open System

open CompleteInformation.Core

[<RequireQualifiedAccess>]
module Api =
    let devErrorHandler (ex: Exception) _ = Propagate(ex.ToString())

    let build devMode api routeBuilder =
        Remoting.createApi ()
        |> if devMode then
               Remoting.withErrorHandler devErrorHandler
           else
               id
        |> Remoting.withRouteBuilder routeBuilder
        |> Remoting.fromValue api
        |> Remoting.buildHttpHandler

open Giraffe.Core

type WebserverPlugin =
    abstract member getApi: bool -> (string -> string -> string) -> (HttpFunc -> HttpContext -> HttpFuncResult)

    abstract member getMetaData: unit -> PluginMetadata
