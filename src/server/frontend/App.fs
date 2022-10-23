namespace CompleteInformation.Core.Frontend.Web

open Elmish
open Elmish.React

module App =
    Program.mkProgram Index.init Index.update Index.view
#if DEBUG
    |> Program.withConsoleTrace
#endif
    |> Program.withReactSynchronous "elmish-app"
    |> Program.run
