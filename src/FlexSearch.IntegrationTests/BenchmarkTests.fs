module BenchmarkTests

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq
open System.Threading.Tasks.Dataflow

//let getWikiIndex() = 
//    let index = new Index()
//    index.IndexName <- "wikipedia"
//    index.Fields.Add
//        ("datetime", new FieldProperties(FieldType = FieldType.DateTime))
//    index.Fields.Add("title", new FieldProperties(FieldType = FieldType.Text))
//    index.Fields.Add
//        ("body", new FieldProperties(FieldType = FieldType.Text, Store = false))
//    index.IndexConfiguration.CommitTimeSec <- 500
//    index.IndexConfiguration.RefreshTimeMilliSec <- 500000
//    index.IndexConfiguration.DirectoryType <- DirectoryType.MemoryMapped
//    index.Online <- true
//    index
//
///// This generates single file based wikipedia dump. It usesthe output of
///// org.apache.lucene.benchmark.utils.ExtractWikipedia as input. In order to use the 
///// lucene class. Use the below command after copying all the lucene jars to the
///// same directory as the wikipedia xml dump file.
///// java -cp lucene-benchmark-4.7.0.jar;* org.apache.lucene.benchmark.utils.ExtractWikipedia -i enwiki-20130904-pages-articles.xml -d true
//let createWikipediaDump (path : string) (outputFile : string) = 
//    for file in System.IO.Directory.GetFiles
//                    (path, "*.txt", System.IO.SearchOption.AllDirectories) do
//        let lines = File.ReadAllLines(file)
//        if lines.Length <> 6 then failwith "Incorrect input file"
//        // Convert wikipedia datetime to our custom format
//        let dateTime = 
//            System.DateTime.Parse(lines.[0]).ToString("yyyyMMddHHmmss")
//        lines.[0] <- dateTime
//        File.AppendAllLines(outputFile, lines)
//    ()
//
//let indexingLunceneWikipediaDumpBenchMarkTests (inputFile : string) (queueService: IQueueService) = 
//    Helpers.nodeState |> IndexService.AddIndex(getWikiIndex()) |> ignore
//    let file = new StreamReader(inputFile)
//    let document = new Dictionary<string, string>()
//    document.Add("datetime", "")
//    document.Add("title", "")
//    document.Add("body", "")
//    let mutable proc = true
//    let mutable i = 1
//    printfn "Starting Test"
//    let stopwatch = new Stopwatch()
//    stopwatch.Start()
//    while proc do
//        let line1 = file.ReadLine()
//        if line1 = null then proc <- false
//        else 
//            document.["datetime"] <- line1
//            file.ReadLine() |> ignore
//            document.["title"] <- file.ReadLine()
//            file.ReadLine() |> ignore
//            document.["body"] <- file.ReadLine()
//            file.ReadLine() |> ignore
//            queueService.AddDocumentQueue("wikipedia",(i.ToString()), document)
//    stopwatch.Stop()
//    let fileInfo = new FileInfo(inputFile)
//    let fileSize = float (fileInfo.Length) / 1073741824.0
//    let time = float (stopwatch.ElapsedMilliseconds) / 3600000.0
//    let indexingSpeed = fileSize / time
//    printfn "Total Records indexed: %i" i
//    printfn "Total Elapsed time (ms): %i" stopwatch.ElapsedMilliseconds
//    printfn "Total Data Size (MB): %f" 
//        (float (fileInfo.Length) / (1024.0 * 1024.0))
//    printfn "Indexing Speed (GB/Hr): %f" indexingSpeed
//    Console.ReadKey()
//
//let createSingleFileFromWikiExtractor (path : string) (outputFile : string) = 
//    let target = new StreamWriter(outputFile)
//    printfn "Starting dump file creation"
//    let files = 
//        System.IO.Directory.GetFiles
//            (path, "*.raw", System.IO.SearchOption.AllDirectories)
//    for file in files do
//        printf "."
//        use reader = new StreamReader(file)
//        let mutable text = reader.ReadLine()
//        while text <> null do
//            let title = text.Substring(1, text.IndexOf('>') - 1)
//            let body = 
//                text.Substring
//                    (text.IndexOf('[') + 1, text.Length - text.IndexOf('[') - 2)
//            target.WriteLine("{0}|{1}", title, body)
//            text <- reader.ReadLine()
//    printfn "Dump file creation complete"
//
//// Test System 1
//// Intel i7-3820 Non-SSD 32 GB Ram
//// Test results 11/03/2014 
//// Total Records indexed: 5184858
//// Total Elapsed time (ms): 597277
//// Total Data Size (MB): 8954.854471
//// Indexing Speed (GB/Hr): 52.709062
//let indexingWikiExtractorDumpBenchMarkTests (inputFile : string) (queueService: IQueueService) =
//    Helpers.nodeState |> IndexService.AddIndex(getWikiIndex()) |> ignore
//    let file = new StreamReader(inputFile)
//    //let document = new Dictionary<string, string>()
//    //document.Add("title", "")
//    //document.Add("body", "")
//    let mutable proc = true
//    let mutable i = 1
//    printfn "Starting Test"
//    let stopwatch = new Stopwatch()
//    stopwatch.Start()
//    while proc do
//        let mutable line = file.ReadLine()
//        if line = null then proc <- false
//        else 
//            let document = new Dictionary<string, string>()
//            document.Add("title", line.Substring(0, line.IndexOf('|') - 1))
//            document.Add("body", line.Substring(line.IndexOf('|') + 1))
//            //document.["title"] <- line.Substring(0, line.IndexOf('|') - 1)
//            //document.["body"] <- line.Substring(line.IndexOf('|') + 1)
//            //Helpers.indexService.PerformCommand("wikipedia", Create(i.ToString(), document)) |> ignore
//            queueService.AddDocumentQueue("wikipedia",(i.ToString()), document)
//            i <- i + 1
//            line <- file.ReadLine()
//    stopwatch.Stop()
//    let fileInfo = new FileInfo(inputFile)
//    let fileSize = float (fileInfo.Length) / 1073741824.0
//    let time = float (stopwatch.ElapsedMilliseconds) / 3600000.0
//    let indexingSpeed = fileSize / time
//    printfn "Total Records indexed: %i" i
//    printfn "Total Elapsed time (ms): %i" stopwatch.ElapsedMilliseconds
//    printfn "Total Data Size (MB): %f" 
//        (float (fileInfo.Length) / (1024.0 * 1024.0))
//    printfn "Indexing Speed (GB/Hr): %f" indexingSpeed
