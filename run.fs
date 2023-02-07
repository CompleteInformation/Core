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

    let testProjectsDotNet = [ "./tests/base/backend/web/" ]

    let dotnetProjects = [ serverBackend; yield! testProjectsDotNet ]
    let fableProjects = [ "./src/base/frontend/web/" ]

    let npmProjects = [ serverFrontend ]

    let allProjects = [ dotnetProjects; npmProjects ] |> Seq.concat

    let packPath = "./pack"
    let publishPath = "./publish"

    // List of plugins which should be downloaded when serving the web app
    let plugins = []

module Task =
    let restore () = job {
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

    let build config = job {
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
        dotnet [ "watch"; "run"; "-p:ExtraDefineConstants=DEVSERVER"; "--project"; Config.serverBackend ]

    let serveWeb () = parallelJob {
        serveWebClient ()
        serveWebServer ()
    }

    let test () = job {
        for project in Config.testProjectsDotNet do
            DotNet.run project
    }

    let publishWithConfig os config fileName = job {
        let tmpPath = $"{Config.publishPath}/tmp"
        Shell.cleanDir tmpPath

        match config with
        | Debug ->
            // Publish selfcontained, but in debug mode
            dotnet [
                "publish"
                Config.serverBackend
                "-r"
                os
                "-v"
                "minimal"
                "-c"
                DotNetConfig.toString Debug
                "-o"
                tmpPath
                "--self-contained"
                "/p:PublishSingleFile=true"
                "/p:EnableCompressionInSingleFile=true"
                "/p:IncludeNativeLibrariesForSelfExtract=true"
                "/p:DebugType=None"
            ]
        | Release ->
            dotnet [
                "publish"
                Config.serverBackend
                "-r"
                os
                "-v"
                "minimal"
                "-c"
                DotNetConfig.toString Release
                "-o"
                tmpPath
                "--self-contained"
                "/p:PublishSingleFile=true"
                "/p:PublishTrimmed=true"
                "/p:EnableCompressionInSingleFile=true"
                "/p:IncludeNativeLibrariesForSelfExtract=true"
                "/p:DebugType=None"
            ]

        // Remove all the *.xml files nobody needs
        Directory.EnumerateFiles(tmpPath, "*.xml") |> Seq.iter (Shell.rm)

        Directory.CreateDirectory $"{tmpPath}/WebRoot" |> ignore

        Shell.copyRecursiveTo false $"{tmpPath}/WebRoot" $"{Config.serverFrontend}/deploy/public"
        |> ignore

        // Remove plugins, if there are some installed for testing
        Shell.deleteDir $"{tmpPath}/WebRoot/plugins"

        // Bundle it up
        let archive = $"./{fileName}.tar.lz"

        for file in Directory.EnumerateFiles(tmpPath, "*", SearchOption.AllDirectories) do
            let filePath = Path.GetRelativePath(tmpPath, file)

            cmd "tar" [ "-ravf"; archive; "-C"; tmpPath; filePath ]

        Shell.mv archive Config.publishPath
    }

    let publish () = job {
        // Cleanup
        Shell.cleanDir Config.publishPath

        // Release
        build Release // At first, we have to build to get the frontend code files
        publishWithConfig (DotNetOS.toString LinuxX64) Release "complete-information-server-linux-x64"
        publishWithConfig "linux-arm" Release "complete-information-server-linux-arm"
        publishWithConfig "linux-arm64" Release "complete-information-server-linux-arm64"

        // Devkit
        build Debug // At first, we have to build to get the frontend code files
        publishWithConfig (DotNetOS.toString LinuxX64) Debug "ci-plugin-devkit-linux-x64"
    }

[<EntryPoint>]
let main args =
    args
    |> List.ofArray
    |> function
        | [ "restore" ] -> Task.restore ()
        | [ "build" ] -> job {
            Task.restore ()
            Task.build Debug
          }
        | [ "serve"; "web" ]
        | [ "serve" ]
        | [] -> job {
            Task.restore ()
            Task.serveWeb ()
          }
        | [ "serve"; "web-server" ] -> job {
            Task.restore ()
            Task.serveWebServer ()
          }
        | [ "serve"; "web-client" ] -> job {
            Task.restore ()
            Task.serveWebClient ()
          }
        | [ "test" ] -> job {
            Task.restore ()
            Task.test ()
          }
        | [ "publish" ] -> job {
            Task.restore ()
            Task.publish ()
          }
        | _ -> Job.error [ "Usage: dotnet run [<command>]"; "Look up available commands in run.fs" ]
    |> Job.execute
