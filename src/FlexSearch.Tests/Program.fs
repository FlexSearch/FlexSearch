open FsUnit
open Fuchu
open System
open FlexSearch.Core
[<EntryPoint>]
let main argv = 
    Logger.StartSession()
    let result = Tests.defaultMainThisAssembly (argv)
    Logger.EndSession()
    // Uncheck the below for debuggin individual test
    //let result = run (HttpModuleTests.testRunHelper())
    //Console.WriteLine(result)
    //Console.ReadKey() |> ignore
    result

