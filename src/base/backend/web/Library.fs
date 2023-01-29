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

    module Internal =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())

        let getFilePath (modul: string) (file: string) =
            let basePath = "./data"
            let path = Path.Combine(basePath, modul, file)
            // Impure, we create the path if it does not exist
            // TODO: Could be done on startup of the server, but that would require
            // the base path in a shared lib, skipped for now
            Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
            path

        let rec handleLoadException (exn: exn) =
            match exn with
            | :? AggregateException as exn ->
                // Do we have only one inner exception, we unwrap and handle that one
                if exn.InnerExceptions.Count = 1 then
                    handleLoadException exn.InnerExceptions.[0]
                else
                    // Otherwise we rethrow the aggregate exception
                    raise exn
            | :? FileNotFoundException -> FileNotFound
            | _ -> raise exn

    module File =
        let append file data =
            File.AppendAllTextAsync(file, $"%s{data}\n") |> Async.AwaitTask

        let appendSeq file data =
            File.AppendAllLinesAsync(file, data) |> Async.AwaitTask

        let readLines file =
            try
                let lines = File.ReadLines file
                Success lines
            with exn ->
                Internal.handleLoadException exn

        let save file data =
            File.WriteAllTextAsync(file, $"%s{data}\n") |> Async.AwaitTask

        let saveSeq file data =
            File.WriteAllLinesAsync(file, data) |> Async.AwaitTask

        let load file = async {
            try
                let! data = File.ReadAllTextAsync file |> Async.AwaitTask
                return Success data
            with exn ->
                return Internal.handleLoadException exn
        }

        let loadSeq file = async {
            try
                let! data = File.ReadAllLinesAsync file |> Async.AwaitTask
                return Success data
            with exn ->
                return Internal.handleLoadException exn
        }

    module Json =
        let save<'a> modul file (data: 'a) = async {
            let file = Internal.getFilePath modul file
            let json = JsonSerializer.Serialize<'a>(data, Internal.options)
            do! File.save file json
        }

        let load<'a> modul file = async {
            let! json = Internal.getFilePath modul file |> File.load

            return ReadResult.map (fun (json: string) -> JsonSerializer.Deserialize<'a>(json, Internal.options)) json
        }

    module JsonL =
        let append<'a> modul file (data: 'a) = async {
            let file = Internal.getFilePath modul file
            let json = JsonSerializer.Serialize<'a>(data, Internal.options)
            do! File.append file json
        }

        let appendSeq<'a> modul file (data: 'a seq) = async {
            let file = Internal.getFilePath modul file

            do!
                data
                |> Seq.map (fun data -> JsonSerializer.Serialize<'a>(data, Internal.options))
                |> File.appendSeq file
        }

        let load<'a> modul file = async {
            let! json = Internal.getFilePath modul file |> File.loadSeq

            return
                ReadResult.map
                    (Seq.map (fun (json: string) -> JsonSerializer.Deserialize<'a>(json, Internal.options)))
                    json
        }

        let read<'a> modul file =
            let result = Internal.getFilePath modul file |> File.readLines

            ReadResult.map
                (Seq.map (fun (json: string) -> JsonSerializer.Deserialize<'a>(json, Internal.options)))
                result

        let save<'a> modul file (data: 'a seq) = async {
            let file = Internal.getFilePath modul file

            do!
                data
                |> Seq.map (fun data -> JsonSerializer.Serialize<'a>(data, Internal.options))
                |> File.saveSeq file
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
