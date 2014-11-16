open Autofac
open FlexSearch.Benchmarks
open FlexSearch.Core
open FlexSearch.Documention
open Nessos.UnionArgParser
open PerfUtil
open System
open FlexSearch.Documention.GenerateExamples
open FlexSearch.Documention

let GenerateGlossary() = 
    
    ReferenceDocumentation.GenerateGlossary()
    printfn "Missing Definition"
    for def in ReferenceDocumentation.missingDefinitions do
        printfn "%s" def
    printfn "Missing def count:%i" (ReferenceDocumentation.missingDefinitions.Count)

type Arguments = 
    | WikipediaIndexingTest1KB of fileName : string
    | WikipediaIndexingTest4KB of fileName : string
    | WikipediaQueryTests of folderPath : string
    | WikipediaQueryTestsLong of folderPath : string
    | GenerateWikipedia1KBDump of path : string * outPutFileName : string
    | GenerateWikipedia4KBDump of path : string * outPutFileName : string
    | GenerateWikipediaQueries of folderPath : string
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | WikipediaIndexingTest1KB _ -> "Wikipedia 1KB file test. (ex: --WikipediaIndexingTest1KB [fileName]"
            | WikipediaIndexingTest4KB _ -> "Wikipedia 4KB file test. (ex: --WikipediaIndexingTest4KB [fileName]"
            | WikipediaQueryTests _ -> "Wikipedia Query Tests over 4KB index. (ex: --WikipediaQueryTests [folderPath]"
            | WikipediaQueryTestsLong _ -> 
                "Wikipedia Query Tests over 4KB index. (ex: --WikipediaQueryTestsLong [folderPath]"
            | GenerateWikipedia1KBDump _ -> 
                "Wikipedia 1KB test file generation. (ex: --GenerateWikipedia1KBDump [folderPath] [outputFile]"
            | GenerateWikipedia4KBDump _ -> 
                "Wikipedia 1KB test file generation. (ex: --GenerateWikipedia4KBDump [folderPath]  [outputFile]"
            | GenerateWikipediaQueries _ -> 
                "Wikipedia test queries generation. (ex: --GenerateWikipediaQueries [folderPath]"

[<EntryPoint>]
let main argv = 
    //GenerateApiDocumentation()
    //GenerateGlossary()
    ReferenceDocumentation.GenerateGlossaryPages()
    //GenerateIndicesExamples()
    //Global.AddIndex()
    
    //WikipediaPerformanceTests.WikipediaQueryTests("G:\wikipedia")
    //WikipediaPerformanceTests.RandomQueryGenerator ("G:\wikipedia") 
    //WikipediaPerformanceTests.WikipediaIndexingTest ("G:\wikipedia\wikidump4KB.txt") true
    let parser = UnionArgParser.Create<Arguments>()
    if argv.Length = 0 then printfn "%s" (parser.Usage("FlexSearch Benchmark Usage:"))
    else 
        let results = parser.Parse(argv)
        let all = results.GetAllResults()
        match all.Head with
        | WikipediaIndexingTest1KB(file) -> WikipediaPerformanceTests.WikipediaIndexingTest file true
        | WikipediaIndexingTest4KB(file) -> WikipediaPerformanceTests.WikipediaIndexingTest file true
        | WikipediaQueryTests(folder) -> WikipediaPerformanceTests.WikipediaQueryTests(folder, false)
        | WikipediaQueryTestsLong(folder) -> WikipediaPerformanceTests.WikipediaQueryTests(folder, true)
        | GenerateWikipedia1KBDump(path, output) -> 
            WikipediaPerformanceTests.CreateWikipediaDumpForWikiExtractor path output 1
        | GenerateWikipedia4KBDump(path, output) -> 
            WikipediaPerformanceTests.CreateWikipediaDumpForWikiExtractor path output 4
        | GenerateWikipediaQueries(folder) -> WikipediaPerformanceTests.RandomQueryGenerator folder
    Console.ReadKey() |> ignore
    0 // return an integer exit code
