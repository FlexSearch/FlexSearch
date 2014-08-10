module WikipediaPerformanceTests

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq
open System.Threading.Tasks.Dataflow
open System.Threading
open System.Threading.Tasks
open Autofac
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.standard
open org.apache.lucene.analysis.tokenattributes
open org.apache.lucene.analysis.util
open org.apache.lucene.index
open org.apache.lucene.queries
open org.apache.lucene.queryparser.classic
open org.apache.lucene.queryparser.flexible
open org.apache.lucene.search
open org.apache.lucene.search.highlight

let GetWikiIndex() = 
    let index = new Index()
    index.IndexName <- "wikipedia"
    index.Fields.Add("datetime", new FieldProperties(FieldType = FieldType.DateTime, Store = false))
    index.Fields.Add("title", new FieldProperties(FieldType = FieldType.Text, Store = false))
    index.Fields.Add("body", new FieldProperties(FieldType = FieldType.Text, Store = false))
    index.IndexConfiguration.CommitTimeSec <- 500
    index.IndexConfiguration.RefreshTimeMilliSec <- 500000
    index.IndexConfiguration.DirectoryType <- DirectoryType.MemoryMapped
    index.Online <- true
    index

/// This generates single file based Wikipedia dump. It uses the output of
/// org.apache.lucene.benchmark.utils.ExtractWikipedia as input. In order to use the 
/// Lucene class. Use the below command after copying all the Lucene jars to the
/// same directory as the Wikipedia xml dump file.
/// java -cp lucene-benchmark-4.7.0.jar;* org.apache.lucene.benchmark.utils.ExtractWikipedia -i enwiki-20130904-pages-articles.xml -d true
/// Obsolete
let CreateWikipediaDump (path : string) (outputFile : string) = 
    for file in System.IO.Directory.GetFiles(path, "*.txt", System.IO.SearchOption.AllDirectories) do
        let lines = File.ReadAllLines(file)
        if lines.Length <> 6 then failwith "Incorrect input file"
        // Convert Wikipedia date time to our custom format
        let dateTime = System.DateTime.Parse(lines.[0]).ToString("yyyyMMddHHmmss")
        lines.[0] <- dateTime
        File.AppendAllLines(outputFile, lines)

/// This generates single file based Wikipedia dump. It uses the output of WikiPedia python extractor
/// and creates 1KB or 4 KB per line documents
let CreateWikipediaDumpForWikiExtractor (path : string) (outputFile : string) (fileSizeKB : int) = 
    let target = new StreamWriter(outputFile)
    printfn "Starting dump file creation"
    let size = 
        if fileSizeKB = 1 then 1024
        else 4096
    
    let sizeOfCharacter = sizeof<char>
    let characterCount = size / sizeOfCharacter
    let files = System.IO.Directory.GetFiles(path, "*.raw", System.IO.SearchOption.AllDirectories)
    for file in files do
        printf "."
        use reader = new StreamReader(file)
        let mutable text = reader.ReadLine()
        while text <> null do
            let title = text.Substring(1, text.IndexOf('>') - 1)
            let body = text.Substring(text.IndexOf('[') + 1, text.Length - text.IndexOf('[') - 2)
            let line = sprintf "%s | %s" title body
            if line.Length >= characterCount then target.WriteLine(line.Substring(0, characterCount))
            text <- reader.ReadLine()
    printfn "Dump file creation complete"

/// <summary>
/// Generates random queries from Wikipedia 4KB corpus
/// </summary>
/// <param name="path"></param>
/// <param name="outputFolder"></param>
let RandomQueryGenerator (filePath : string) (outputFolder : string) (queriesToGenerate : int) 
    (indexService : IIndexService) = 
    indexService.AddIndex(GetWikiIndex()) |> ignore
    Thread.Sleep(1000)
    let searchers = 
        match indexService.GetIndexSearchers("wikipedia") with
        | Choice1Of2(s) -> s
        | _ -> failwithf "Unable to get the searchers"
    
    let highFreq = new ResizeArray<string>()
    let highFreqTerms = 
        org.apache.lucene.misc.HighFreqTerms.getHighFreqTerms 
            (searchers.[0].getIndexReader(), 10000, "body[lucene_4_9]<lucene_4_1>", 
             new org.apache.lucene.misc.HighFreqTerms.DocFreqComparator())
    for freqTerm in highFreqTerms do
        let term = freqTerm.termtext.utf8ToString().Replace("u'", "").Replace("'", "")
        highFreq.Add(term)
    use termQueryFile = new StreamWriter(Path.Combine(outputFolder, "TermQueries.txt"))
    use bqAndHighHighFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesAndHighHigh.txt"))
    use bqAndHighMedFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesAndHighMed.txt"))
    use bqOrHighHighFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesOrHighHigh.txt"))
    use bqOrHighMedFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesOrHighMed.txt"))
    use fuzzy1File = new StreamWriter(Path.Combine(outputFolder, "Fuzzy1Queries.txt"))
    use fuzzy2File = new StreamWriter(Path.Combine(outputFolder, "Fuzzy2Queries.txt"))
    use wildcardFile = new StreamWriter(Path.Combine(outputFolder, "WildCardQueries.txt"))
    use phraseQueryFile = new StreamWriter(Path.Combine(outputFolder, "PhraseQueries.txt"))
    for n in 0..5000 - 1 do
        termQueryFile.WriteLine(sprintf "body = '%s'" highFreq.[n])
        fuzzy1File.WriteLine(sprintf "body ~= '%s' {slop : '1'}" highFreq.[n])
        fuzzy2File.WriteLine(sprintf "body ~= '%s' {slop : '2'}" highFreq.[n])
        if highFreq.[n].Length > 2 then
            wildcardFile.WriteLine(sprintf "body like '%s*'" (highFreq.[n].Substring(0, 2)))
        bqAndHighHighFile.WriteLine(sprintf "body = '%s' AND body = '%s'" highFreq.[n] highFreq.[n + 1])
        bqAndHighMedFile.WriteLine(sprintf "body = '%s' AND body = '%s'" highFreq.[n] highFreq.[7000 - n])
        bqOrHighHighFile.WriteLine(sprintf "body = '%s' OR body = '%s'" highFreq.[n] highFreq.[n + 1])
        bqOrHighMedFile.WriteLine(sprintf "body = '%s' OR body = '%s'" highFreq.[n] highFreq.[7000 - n])

// Test System 1
// Intel i7-3820 Non-SSD 32 GB Ram
// Test results 11/03/2014 
// Total Records indexed: 5184858
// Total Elapsed time (ms): 597277
// Total Data Size (MB): 8954.854471
// Indexing Speed (GB/Hr): 52.709062
// Test results 02/08/2014 Windows 8.1
// Total Records indexed: 5184858
// Total Elapsed time (ms): 345886
// Total Data Size (MB): 8954.854471
// Indexing Speed (GB/Hr): 91.018169
let IndexingWikiExtractorDumpBenchMarkTests (inputFile : string) (indexService : IIndexService) 
    (queueService : IQueueService) = 
    indexService.AddIndex(GetWikiIndex()) |> ignore
    let file = new StreamReader(inputFile)
    let mutable proc = true
    let mutable i = 1
    printfn "Starting Test"
    let stopwatch = new Stopwatch()
    stopwatch.Start()
    while proc do
        let mutable line = file.ReadLine()
        if line = null then proc <- false
        else 
            let document = new Dictionary<string, string>()
            document.Add("title", line.Substring(0, line.IndexOf('|') - 1))
            document.Add("body", line.Substring(line.IndexOf('|') + 1))
            //document.Add("body", line)
            queueService.AddDocumentQueue("wikipedia", (i.ToString()), document)
            i <- i + 1
            line <- file.ReadLine()
    indexService.Commit("wikipedia") |> ignore
    stopwatch.Stop()
    let fileInfo = new FileInfo(inputFile)
    let fileSize = float (fileInfo.Length) / 1073741824.0
    let time = float (stopwatch.ElapsedMilliseconds) / 3600000.0
    let indexingSpeed = fileSize / time
    printfn "Total Records indexed: %i" i
    printfn "Total Elapsed time (ms): %i" stopwatch.ElapsedMilliseconds
    printfn "Total Data Size (MB): %f" (float (fileInfo.Length) / (1024.0 * 1024.0))
    printfn "Indexing Speed (GB/Hr): %f" indexingSpeed

let ExecuteQuery (queries : string []) (searchService : ISearchService) = 
    Parallel.ForEach(queries, 
                     (fun n -> 
                     try 
                         match searchService.Search(new SearchQuery("wikipedia", n)) with
                         | Choice1Of2(a) -> assert (a.TotalAvailable > 100)
                         | Choice2Of2(e) -> printfn "Error: %s Query:%s" e.UserMessage n
                     with e -> printfn "%A" e
                     ()))
    |> ignore
    ()
