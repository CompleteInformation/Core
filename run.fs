open Fake.Core

open RunHelpers
open RunHelpers.Shortcuts
open RunHelpers.Templates

[<RequireQualifiedAccess>]
module Config =
    let backendProject = $"backend/web/src/WebBackend.fsproj"

module Task =
    let restore () =
        job {
            DotNet.restoreWithTools Config.backendProject

            CreateProcess.fromRawCommand "npm" [ "install" ]
            |> CreateProcess.withWorkingDirectory "./frontend/web"
            |> Job.fromCreateProcess
        }

    let buildWebServer () =
        DotNet.build Config.backendProject Debug

    let build () = job { buildWebServer () }

    let serveWebClient () =
        CreateProcess.fromRawCommand "npm" [ "run"; "start" ]
        |> CreateProcess.withWorkingDirectory "./frontend/web"
        |> Job.fromCreateProcess

    let serveWebServer () =
        dotnet [
            "watch"
            "run"
            "--project"
            Config.backendProject
        ]

    let serveWeb () =
        parallelJob {
            serveWebClient ()
            serveWebServer ()
        }

[<EntryPoint>]
let main args =
    args
    |> List.ofArray
    |> function
        | [ "restore" ] -> Task.restore ()
        | [ "build" ] ->
            job {
                Task.restore ()
                Task.build ()
            }
        | [ "serve"; "web" ]
        | [ "serve" ]
        | [] ->
            job {
                Task.restore ()
                Task.serveWeb ()
            }
        | _ ->
            Job.error [
                "Usage: dotnet run [<command>]"
                "Look up available commands in run.fs"
            ]
    |> Job.execute
