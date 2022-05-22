open Fake.Core
open Fake.IO

open RunHelpers
open RunHelpers.Shortcuts
open RunHelpers.Templates

open System.IO

[<RequireQualifiedAccess>]
module Config =
    let backendProject = "backend/web/WebBackend.fsproj"
    let frontendProject = "frontend/web/WebFrontend.fsproj"
    let frontendDeployPath = "frontend/web/deploy"
    let packPath = "./pack"
    let publishPath = "./publish"

module Task =
    let restore () =
        job {
            DotNet.restoreWithTools Config.backendProject

            CreateProcess.fromRawCommand "npm" [ "install" ]
            |> CreateProcess.withWorkingDirectory "./frontend/web"
            |> Job.fromCreateProcess
        }

    let buildWebClient config =
        let cmd =
            match config with
            | Debug -> "build"
            | Release -> "build-prod"

        CreateProcess.fromRawCommand "npm" [ "run"; cmd ]
        |> CreateProcess.withWorkingDirectory "./frontend/web"
        |> Job.fromCreateProcess

    let buildWebServer config =
        DotNet.build Config.backendProject config

    let build config =
        job {
            buildWebClient config
            buildWebServer config
        }

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

    let publish () =
        job {
            // Cleanup
            [
                Config.publishPath
                Config.frontendDeployPath
            ]
            |> Shell.cleanDirs

            DotNet.publishSelfContained Config.publishPath Config.backendProject LinuxX64

            // Remove all the *.xml files nobody needs
            Directory.EnumerateFiles(Config.publishPath, "*.xml")
            |> Seq.iter (Shell.rm)

            // Build frontend and copy it to webroot
            buildWebClient Release

            Directory.CreateDirectory $"{Config.publishPath}/WebRoot"
            |> ignore

            Shell.copyRecursiveTo false $"{Config.publishPath}/WebRoot" $"{Config.frontendDeployPath}/public"
            |> ignore

            // Bundle it up
            let archive = "./CompleteInformation.tar.lz"

            for file in Directory.EnumerateFiles(Config.publishPath, "*", SearchOption.AllDirectories) do
                let filePath = Path.GetRelativePath(Config.publishPath, file)

                cmd
                    "tar"
                    [
                        "-ravf"
                        archive
                        "-C"
                        Config.publishPath
                        filePath
                    ]

            Shell.cleanDir Config.publishPath
            Shell.mv archive Config.publishPath
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
                Task.build Debug
            }
        | [ "serve"; "web" ]
        | [ "serve" ]
        | [] ->
            job {
                Task.restore ()
                Task.serveWeb ()
            }
        | [ "serve"; "web-server" ] ->
            job {
                Task.restore ()
                Task.serveWebServer ()
            }
        | [ "publish" ] ->
            job {
                Task.restore ()
                Task.publish ()
            }
        | _ ->
            Job.error [
                "Usage: dotnet run [<command>]"
                "Look up available commands in run.fs"
            ]
    |> Job.execute
