open System
open FlexSearch.Documention
open FlexSearch.Core
open Autofac
open Nessos.UnionArgParser
open Agdur

let GenerateGlossary() = 
    ReferenceDocumentation.GenerateGlossary()
    printfn "Missing Definition"
    for def in ReferenceDocumentation.missingDefinitions do
        printfn "%s" def
    printfn "Missing def count:%i" (ReferenceDocumentation.missingDefinitions.Count)

type Arguments = 
    | WikipediaTest1KB of fileName : string
    | WikipediaTest4KB of fileName : string
    | GenerateWikipedia1KBDump of path : string * outPutFileName : string
    | GenerateWikipedia4KBDump of path : string * outPutFileName : string
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | WikipediaTest1KB _ -> "Wikipedia 1KB file test. (ex: --WikipediaTest1KB [fileName]"
            | WikipediaTest4KB _ -> "Wikipedia 4KB file test. (ex: --WikipediaTest4KB [fileName]"
            | GenerateWikipedia1KBDump _ -> 
                "Wikipedia 1KB test file generation. (ex: --GenerateWikipedia1KBDump [folderPath] [outputFile]"
            | GenerateWikipedia4KBDump _ -> 
                "Wikipedia 1KB test file generation. (ex: --GenerateWikipedia4KBDump [folderPath]  [outputFile]"

[<EntryPoint>]
let main argv = 
    let searchService = GenerateExamples.Container.Resolve<ISearchService>()
    let indexService = GenerateExamples.Container.Resolve<IIndexService>()
//    WikipediaPerformanceTests.RandomQueryGenerator "" "F:\wikipedia" 100 (GenerateExamples.Container.Resolve<IIndexService>()) 

    GenerateExamples.Container.Resolve<IIndexService>().AddIndex(WikipediaPerformanceTests.GetWikiIndex()) |> ignore
    System.Threading.Thread.Sleep(1000)
    let queries = System.IO.File.ReadAllLines("F:\wikipedia\TermQueries.txt")
    Benchmark.This(fun _ -> WikipediaPerformanceTests.ExecuteQuery queries searchService).Times(10).Average()
             .InMilliseconds().Max().InMilliseconds().Min().InMilliseconds().ToConsole().AsFormattedString()

    //WikipediaPerformanceTests.CreateWikipediaDumpForWikiExtractor "F:\wikipedia\extracted" "F:\wikipedia\wikidump1KB.txt" 1
    //WikipediaPerformanceTests.CreateWikipediaDumpForWikiExtractor "F:\wikipedia\extracted" "F:\wikipedia\wikidump4KB.txt" 4 
    //WikipediaPerformanceTests.IndexingWikiExtractorDumpBenchMarkTests "F:\wikipedia\wikidump4KB.txt" (GenerateExamples.Container.Resolve<IIndexService>()) (GenerateExamples.Container.Resolve<IQueueService>())
    //GenerateGlossary()
    //GenerateExamples.GenerateIndicesExamples()
    let parser = UnionArgParser<Arguments>()
    if argv.Length = 0 then printfn "%s" (parser.Usage("FlexSearch Benchmark Usage:"))
    else 
        let results = parser.Parse(argv)
        let all = results.GetAllResults()
        match all.Head with
        | WikipediaTest1KB(file) -> 
            WikipediaPerformanceTests.IndexingWikiExtractorDumpBenchMarkTests file 
                (GenerateExamples.Container.Resolve<IIndexService>()) 
                (GenerateExamples.Container.Resolve<IQueueService>())
        | WikipediaTest4KB(file) -> 
            WikipediaPerformanceTests.IndexingWikiExtractorDumpBenchMarkTests file 
                (GenerateExamples.Container.Resolve<IIndexService>()) 
                (GenerateExamples.Container.Resolve<IQueueService>())
        | GenerateWikipedia1KBDump(path, output) -> 
            WikipediaPerformanceTests.CreateWikipediaDumpForWikiExtractor path output 1
        | GenerateWikipedia4KBDump(path, output) -> 
            WikipediaPerformanceTests.CreateWikipediaDumpForWikiExtractor path output 4
    Console.ReadKey() |> ignore
    0 // return an integer exit code
