open Fake.Core
open Fake.IO

open RunHelpers
open RunHelpers.Shortcuts
open RunHelpers.Templates

open System.IO

[<RequireQualifiedAccess>]
module Config =
    let serverBackend = "./src/server/backend/"
    let serverFrontend = "./src/server/frontend/"

    let dotnetProjects = [ serverBackend ]
    let fableProjects = [ "./src/base/frontend/web/" ]

    let npmProjects = [ serverFrontend ]

    let allProjects = [ dotnetProjects; npmProjects ] |> Seq.concat

    let packPath = "./pack"
    let publishPath = "./publish"

    // List of plugins which should be downloaded when serving the web app
    let plugins = []

module Task =
    let restore () =
        job {
            DotNet.toolRestore ()

            // Restore via dotnet
            for project in Config.allProjects do
                DotNet.restore project

            // Restore via npm
            for project in Config.npmProjects do
                CreateProcess.fromRawCommand "npm" [ "install" ]
                |> CreateProcess.withWorkingDirectory project
                |> Job.fromCreateProcess
        }

    let build config =
        job {
            // Build dotnet projects
            for project in Config.dotnetProjects do
                DotNet.build project config

            // Build fable projects
            for project in Config.fableProjects do
                CreateProcess.fromRawCommand "dotnet" [ "fable"; "-o"; "output" ]
                |> CreateProcess.withWorkingDirectory project
                |> Job.fromCreateProcess

            // Build npm projects
            let cmd =
                match config with
                | Debug -> "build"
                | Release -> "build-prod"

            for project in Config.npmProjects do
                CreateProcess.fromRawCommand "npm" [ "run"; cmd ]
                |> CreateProcess.withWorkingDirectory project
                |> Job.fromCreateProcess
        }

    let serveWebClient () =
        CreateProcess.fromRawCommand "npm" [ "run"; "start" ]
        |> CreateProcess.withWorkingDirectory Config.serverFrontend
        |> Job.fromCreateProcess

    let serveWebServer () =
        dotnet [ "watch"; "run"; "--project"; Config.serverBackend ]

    let serveWeb () =
        parallelJob {
            serveWebClient ()
            serveWebServer ()
        }

    let publish () =
        job {
            // Cleanup
            Shell.cleanDir Config.publishPath

            DotNet.publishSelfContained Config.publishPath Config.serverBackend LinuxX64

            // Remove all the *.xml files nobody needs
            Directory.EnumerateFiles(Config.publishPath, "*.xml") |> Seq.iter (Shell.rm)

            Directory.CreateDirectory $"{Config.publishPath}/WebRoot" |> ignore

            Shell.copyRecursiveTo false $"{Config.publishPath}/WebRoot" $"{Config.serverFrontend}/deploy/public"
            |> ignore

            // Remove plugins, if there are some installed for testing
            Shell.deleteDir $"{Config.publishPath}/WebRoot/plugins"

            // Bundle it up
            let archive = "./CompleteInformation.tar.lz"

            for file in Directory.EnumerateFiles(Config.publishPath, "*", SearchOption.AllDirectories) do
                let filePath = Path.GetRelativePath(Config.publishPath, file)

                cmd "tar" [ "-ravf"; archive; "-C"; Config.publishPath; filePath ]

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
        | [ "serve"; "web-client" ] ->
            job {
                Task.restore ()
                Task.serveWebClient ()
            }
        | [ "publish" ] ->
            job {
                Task.restore ()
                Task.build Release
                Task.publish ()
            }
        | _ -> Job.error [ "Usage: dotnet run [<command>]"; "Look up available commands in run.fs" ]
    |> Job.execute
