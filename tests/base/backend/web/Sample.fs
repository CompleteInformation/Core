module Tests

open Expecto
open System.IO

open CompleteInformation.Base.Backend.Web

[<Tests>]
let tests =
    testList "Persistence Tests" [
        testAsync "file does not exist" {
            let path = Path.GetTempFileName()
            File.Delete path
            let! result = Persistence.File.load path
            Expect.equal result Persistence.FileNotFound "file does not exist"
        }
    ]
