open FsUnit
open Fuchu
open System

[<EntryPoint>]
let main argv = 
    let result = Tests.defaultMainThisAssembly (argv)
    // Uncheck for wikipedia based performance test
    //BenchmarkTests.createSingleFileFromWikiExtractor "F:\wikipedia\extracted" "F:\wikipedia\wikidump.txt"
    //BenchmarkTests.indexingWikiExtractorDumpBenchMarkTests "F:\wikipedia\wikidump.txt"
    
    // Uncheck the below for debugging individual test
    //let result = run (SearchTests.simpleSortingTests())
    //Console.WriteLine(result)
    
    Console.ReadKey() |> ignore
    //result
    0
