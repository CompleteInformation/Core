namespace CompleteInformation.Base.Backend.Web

open Fable.Remoting.Giraffe
open Fable.Remoting.Server
open Microsoft.AspNetCore.Http
open System
open System.IO
open System.Text.Json

open CompleteInformation.Core

[<RequireQualifiedAccess>]
module Persistence =
    let saveFile file content = File.WriteAllTextAsync(file, content)

    let loadFile file = File.ReadAllTextAsync file

    let saveJson<'a> file (data: 'a) =
        task {
            let json = JsonSerializer.Serialize<'a> data
            do! saveFile file json
        }

    let loadJson<'a> file =
        task {
            let! json = loadFile file
            return JsonSerializer.Deserialize<'a> json
        }

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
