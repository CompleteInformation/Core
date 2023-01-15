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

    let getFilePath (modul: string) (file: string) =
        let basePath = "./data"
        let path = Path.Combine(basePath, modul, file)
        // Impure, we create the path if it does not exist
        // Could be done on startup of the server, but that would require
        // the base path in a shared lib, skipped for now
        Directory.CreateDirectory(Path.GetDirectoryName(path))
        path

    let saveJson<'a> modul file (data: 'a) = async {
        let file = getFilePath modul file
        let json = JsonSerializer.Serialize<'a>(data, options)
        do! saveFile file json
    }

    let loadJson<'a> modul file = async {
        let! json = getFilePath modul file |> loadFile
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
