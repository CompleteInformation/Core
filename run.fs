open RunHelpers
open RunHelpers.BasicShortcuts
open RunHelpers.Templates

[<RequireQualifiedAccess>]
module Config =
    let backendProject = $"backend/web/src/WebBackend.fsproj"

module Task =
    let restore () =
        DotNet.restoreWithTools Config.backendProject

    let buildWebServer () =
        DotNet.build Config.backendProject Debug

    let build () = job { buildWebServer () }
    (*let serveWebClient () =
        executeShellCommand "cd frontend/web && npm run start"*)

    let serveWebServer () =
        dotnet [
            "watch"
            "run"
            "--project"
            Config.backendProject
        ]

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
        | [] -> Task.serveWebServer ()
        | _ ->
            let msg =
                [ "Usage: dotnet run [<command>]"
                  "Look up available commands in run.fs" ]

            Error(1, msg)
    |> ProcessResult.wrapUp
