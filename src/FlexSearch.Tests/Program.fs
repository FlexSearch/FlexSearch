open FsUnit
open Fuchu
open System
open FlexSearch.Core
[<EntryPoint>]
let main argv = 
    //let result = Tests.defaultMainThisAssembly (argv)
    
    HttpDocumentation.generateDocumentation()
    // Uncheck for wikipedia based performance test
    //BenchmarkTests.createSingleFileFromWikiExtractor "F:\wikipedia\extracted" "F:\wikipedia\wikidump.txt"
    //BenchmarkTests.indexingWikiExtractorDumpBenchMarkTests "F:\wikipedia\wikidump.txt"
    
    // Uncheck the below for debuggin individual test
    //let result = run (HttpModuleTests.testRunHelper())
    //Console.WriteLine(result)
    
    Console.ReadKey() |> ignore
    //result
    0
