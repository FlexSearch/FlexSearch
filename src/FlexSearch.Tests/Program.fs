// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open FsUnit
open Fuchu
open System
//
//let simpleParameterizedTest = 
//    testList "A simple test" [
//        for c in [1; 2; 3] ->
//            testCase ("Test " + c.ToString()) <| fun _ -> 
//                if c = 3 then
//                    failwith "c = 3 not valid"
//                c |> should equal c
//    ]


[<EntryPoint>]
let main argv = 
    //let result = Tests.defaultMainThisAssembly(argv)
    
    // Uncheck the below for debuggin individual test
    let result = run SocketServerTests.socketServerTests
    
    Console.WriteLine(result)
    Console.ReadKey() |> ignore
    result
    