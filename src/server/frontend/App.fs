namespace CompleteInformation.Core.Frontend.Web

open Elmish
open Elmish.React

#if DEBUG
open Elmish.HMR
#endif

module App =
    Program.mkProgram Index.init Index.update Index.view
#if DEBUG
    |> Program.withConsoleTrace
#endif
    |> Program.withReactSynchronous "elmish-app"
    |> Program.run
