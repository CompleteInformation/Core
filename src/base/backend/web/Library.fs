namespace CompleteInformation.Base.Backend.Web

open Fable.Remoting.Giraffe
open Fable.Remoting.Server
open Microsoft.AspNetCore.Http
open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

open CompleteInformation.Core

[<RequireQualifiedAccess>]
module Persistence =
    type ReadResult<'a> =
        | Success of 'a
        | FileNotFound

    module ReadResult =
        let map result =
            function
            | Success x -> Success(result x)
            | FileNotFound -> FileNotFound

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    let saveFile file content =
        File.WriteAllTextAsync(file, content) |> Async.AwaitTask

    let loadFile file =
        let rec handleException (exn: exn) =
            match exn with
            | :? AggregateException as exn ->
                // Do we have only one inner exception, we unwrap and handle that one
                if exn.InnerExceptions.Count = 1 then
                    handleException exn.InnerExceptions.[0]
                else
                    // Otherwise we rethrow the aggregate exception
                    raise exn
            | :? FileNotFoundException -> FileNotFound
            | _ -> raise exn

        async {
            try
                let! content = File.ReadAllTextAsync file |> Async.AwaitTask
                return Success content
            with exn ->
                return handleException exn
        }

    let saveJson<'a> file (data: 'a) =
        async {
            let json = JsonSerializer.Serialize<'a>(data, options)
            do! saveFile file json
        }

    let loadJson<'a> file =
        async {
            let! json = loadFile file
            return ReadResult.map (fun (json: string) -> JsonSerializer.Deserialize<'a>(json, options)) json
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
