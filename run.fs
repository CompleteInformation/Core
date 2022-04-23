open Fake.Core

open RunHelpers
open RunHelpers.BasicShortcuts
open RunHelpers.Templates

[<RequireQualifiedAccess>]
module Config =
    let backendProject = $"backend/web/src/WebBackend.fsproj"

let startAsJob errorCode (proc: CreateProcess<ProcessResult<unit>>) =
    printfn $"> %s{proc.CommandLine}"
    let task = proc |> Proc.start

    async {
        let! result = task |> Async.AwaitTask

        return
            match result.ExitCode with
            | 0 -> Ok
            | _ -> Error(errorCode, [])
    }

module Task =
    let restore () =
        DotNet.restoreWithTools Config.backendProject

    let buildWebServer () =
        DotNet.build Config.backendProject Debug

    let build () = job { buildWebServer () }

    let serveWebClient () =
        CreateProcess.fromRawCommand "npm" [ "run"; "start" ]
        |> CreateProcess.withWorkingDirectory "frontend/web"
        |> Job.fromCreateProcess Constant.errorExitCode

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
        | [] -> Task.serveWeb ()
        | _ ->
            let msg =
                [ "Usage: dotnet run [<command>]"
                  "Look up available commands in run.fs" ]

            Job.error 1 msg
    |> Job.execute
