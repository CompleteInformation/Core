open System.Diagnostics

type CommandResult = {
  exitCode: int;
}

let executeCommand executable args =
  async {
    let startInfo = ProcessStartInfo()
    startInfo.FileName <- executable
    for a in args do
      startInfo.ArgumentList.Add(a)
    startInfo.RedirectStandardOutput <- false
    startInfo.RedirectStandardError <- false
    startInfo.UseShellExecute <- false
    startInfo.CreateNoWindow <- true
    use p = new Process()
    p.StartInfo <- startInfo
    p.Start() |> ignore

    do! p.WaitForExitAsync() |> Async.AwaitTask
    return {
      exitCode = p.ExitCode;
    }
  }

let executeShellCommand command =
  executeCommand "/usr/bin/env" [ "-S"; "bash"; "-c"; command ]

let serveWebClient () =
    executeShellCommand "cd frontend/web && npm run start"

let serveWebServer () =
    executeShellCommand "cd backend/web/src && dotnet watch run"

let serveWeb () =
    [ serveWebClient (); serveWebServer () ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | cmd :: args ->
        match cmd, args with
        | "serve", [ "web" ]
        | "serve", [] ->
            serveWeb ()
        | _ -> printfn "Unknown command/arg combination"
    | _ -> printfn "Enter command"
    0 // return an integer exit code
