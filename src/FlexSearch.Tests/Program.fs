open FsUnit
open Fuchu
open System

[<EntryPoint>]
let main argv = 
    let result = Tests.defaultMainThisAssembly (argv)
    // Uncheck the below for debuggin individual test
    //let result = run SearchTests.simpleSearchProfileTests
    Console.WriteLine(result)
    Console.ReadKey() |> ignore
    result

