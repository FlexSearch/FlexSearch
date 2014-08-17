namespace FlexSearch.Benchmarks

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
open PerfUtil

module WikipediaPerformanceTests = 
    let QueriesCount = 1000
    
    /// <summary>
    /// Wikipedia article details
    /// </summary>
    type WikiArticle = 
        { Id : string
          Title : string
          Body : string }
    
    /// <summary>
    /// Wikipedia term frequency information
    /// </summary>
    type TermFrequencyInfomation = 
        { Term : string
          TermFrequency : int64
          DocFrequency : int }
    
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
        let characterCount = size // / sizeOfCharacter // Actually sizeof<char> is 2 bytes in UTF
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
    let RandomQueryGenerator(outputFolder : string) = 
        let searchers = 
            match Global.IndexService.GetIndexSearchers(Global.WikiIndexName) with
            | Choice1Of2(s) -> s
            | _ -> failwithf "Unable to get the searchers"
        
        // Term with highest freq is considered top end of the range and is marked as 100%
        // Based on that scale
        // > 1 million High
        // 100k - 1 million  Medium
        // 10k - 100k  Low 
        let highFreqTerms = new ResizeArray<TermFrequencyInfomation>()
        let medFreqTerms = new ResizeArray<TermFrequencyInfomation>()
        let lowFreqTerms = new ResizeArray<TermFrequencyInfomation>()
        let termFreq = 
            org.apache.lucene.misc.HighFreqTerms.getHighFreqTerms 
                (searchers.[0].getIndexReader(), 10000, "body[lucene_4_9]<lucene_4_1>", 
                 new org.apache.lucene.misc.HighFreqTerms.DocFreqComparator())
        let medFreqUpperRange = 1000000
        let medFreqLowerRange = 100000
        let lowFreqLowerRange = 10000
        // Divide the terms in 3 ranges
        for freqTerm in termFreq do
            let term = freqTerm.termtext.utf8ToString().Replace("'", "\\'")
            match freqTerm.docFreq with
            | x when x >= medFreqUpperRange -> 
                highFreqTerms.Add({ Term = term
                                    DocFrequency = freqTerm.docFreq
                                    TermFrequency = freqTerm.totalTermFreq })
            | x when x >= medFreqLowerRange && x < medFreqUpperRange -> 
                medFreqTerms.Add({ Term = term
                                   DocFrequency = freqTerm.docFreq
                                   TermFrequency = freqTerm.totalTermFreq })
            | x when x >= lowFreqLowerRange && x < medFreqLowerRange -> 
                lowFreqTerms.Add({ Term = term
                                   DocFrequency = freqTerm.docFreq
                                   TermFrequency = freqTerm.totalTermFreq })
            | _ -> ()
        use termQueryHighFile = new StreamWriter(Path.Combine(outputFolder, "TermQueriesHigh.txt"))
        use termQueryMedFile = new StreamWriter(Path.Combine(outputFolder, "TermQueriesMed.txt"))
        use termQueryLowFile = new StreamWriter(Path.Combine(outputFolder, "TermQueriesLow.txt"))
        use bqAndHighHighFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesAndHighHigh.txt"))
        use bqAndHighMedFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesAndHighMed.txt"))
        use bqAndHighLowFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesAndHighLow.txt"))
        use bqAndMedMedFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesAndMedMed.txt"))
        use bqAndMedLowFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesAndMedLow.txt"))
        use bqAndLowLowFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesAndLowLow.txt"))
        use bqOrHighHighFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesOrHighHigh.txt"))
        use bqOrHighMedFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesOrHighMed.txt"))
        use bqOrHighLowFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesOrHighLow.txt"))
        use bqOrMedMedFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesOrMedMed.txt"))
        use bqOrMedLowFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesOrMedLow.txt"))
        use bqOrLowLowFile = new StreamWriter(Path.Combine(outputFolder, "BooleanQueriesOrLowLow.txt"))
        use fuzzy1HighFile = new StreamWriter(Path.Combine(outputFolder, "Fuzzy1QueriesHigh.txt"))
        use fuzzy1MedFile = new StreamWriter(Path.Combine(outputFolder, "Fuzzy1QueriesMed.txt"))
        use fuzzy1LowFile = new StreamWriter(Path.Combine(outputFolder, "Fuzzy1QueriesLow.txt"))
        use fuzzy2HighFile = new StreamWriter(Path.Combine(outputFolder, "Fuzzy2QueriesHigh.txt"))
        use fuzzy2MedFile = new StreamWriter(Path.Combine(outputFolder, "Fuzzy2QueriesMed.txt"))
        use fuzzy2LowFile = new StreamWriter(Path.Combine(outputFolder, "Fuzzy2QueriesLow.txt"))
        use wildcardHighFile = new StreamWriter(Path.Combine(outputFolder, "WildCardQueriesHigh.txt"))
        use wildcardMedFile = new StreamWriter(Path.Combine(outputFolder, "WildCardQueriesMed.txt"))
        use wildcardLowFile = new StreamWriter(Path.Combine(outputFolder, "WildCardQueriesLow.txt"))
        //use phraseQueryHighFile = new StreamWriter(Path.Combine(outputFolder, "PhraseQueriesHigh.txt"))
        for n in 0..QueriesCount - 1 do
            termQueryHighFile.WriteLine(sprintf "body = '%s'" highFreqTerms.[n % highFreqTerms.Count].Term)
            termQueryMedFile.WriteLine(sprintf "body = '%s'" medFreqTerms.[n % medFreqTerms.Count].Term)
            termQueryLowFile.WriteLine(sprintf "body = '%s'" lowFreqTerms.[n % lowFreqTerms.Count].Term)
            fuzzy1HighFile.WriteLine(sprintf "body ~= '%s' {slop : '1'}" highFreqTerms.[n % highFreqTerms.Count].Term)
            fuzzy1MedFile.WriteLine(sprintf "body ~= '%s' {slop : '1'}" medFreqTerms.[n % medFreqTerms.Count].Term)
            fuzzy1LowFile.WriteLine(sprintf "body ~= '%s' {slop : '1'}" lowFreqTerms.[n % lowFreqTerms.Count].Term)
            fuzzy2HighFile.WriteLine(sprintf "body ~= '%s' {slop : '2'}" highFreqTerms.[n % highFreqTerms.Count].Term)
            fuzzy2MedFile.WriteLine(sprintf "body ~= '%s' {slop : '2'}" medFreqTerms.[n % medFreqTerms.Count].Term)
            fuzzy2LowFile.WriteLine(sprintf "body ~= '%s' {slop : '2'}" lowFreqTerms.[n % lowFreqTerms.Count].Term)
            if highFreqTerms.[n % highFreqTerms.Count].Term.Length > 2 then 
                wildcardHighFile.WriteLine
                    (sprintf "body like '%s*'" (highFreqTerms.[n % highFreqTerms.Count].Term.Substring(0, 2)))
            if medFreqTerms.[n % medFreqTerms.Count].Term.Length > 2 then 
                wildcardMedFile.WriteLine
                    (sprintf "body like '%s*'" (medFreqTerms.[n % medFreqTerms.Count].Term.Substring(0, 2)))
            if lowFreqTerms.[n % lowFreqTerms.Count].Term.Length > 2 then 
                wildcardLowFile.WriteLine
                    (sprintf "body like '%s*'" (lowFreqTerms.[n % lowFreqTerms.Count].Term.Substring(0, 2)))
            bqAndHighHighFile.WriteLine
                (sprintf "body = '%s' AND body = '%s'" highFreqTerms.[n % highFreqTerms.Count].Term 
                     highFreqTerms.[(n + 1) % highFreqTerms.Count].Term)
            bqAndHighMedFile.WriteLine
                (sprintf "body = '%s' AND body = '%s'" highFreqTerms.[n % highFreqTerms.Count].Term 
                     medFreqTerms.[n % medFreqTerms.Count].Term)
            bqAndHighLowFile.WriteLine
                (sprintf "body = '%s' AND body = '%s'" highFreqTerms.[n % highFreqTerms.Count].Term 
                     lowFreqTerms.[n % lowFreqTerms.Count].Term)
            bqAndMedMedFile.WriteLine
                (sprintf "body = '%s' AND body = '%s'" medFreqTerms.[n % medFreqTerms.Count].Term 
                     medFreqTerms.[(n + 1) % medFreqTerms.Count].Term)
            bqAndMedLowFile.WriteLine
                (sprintf "body = '%s' AND body = '%s'" medFreqTerms.[n % medFreqTerms.Count].Term 
                     lowFreqTerms.[n % lowFreqTerms.Count].Term)
            bqAndLowLowFile.WriteLine
                (sprintf "body = '%s' AND body = '%s'" lowFreqTerms.[n % lowFreqTerms.Count].Term 
                     lowFreqTerms.[(n + 1) % lowFreqTerms.Count].Term)
            bqOrHighHighFile.WriteLine
                (sprintf "body = '%s' OR body = '%s'" highFreqTerms.[n % highFreqTerms.Count].Term 
                     highFreqTerms.[(n + 1) % highFreqTerms.Count].Term)
            bqOrHighMedFile.WriteLine
                (sprintf "body = '%s' OR body = '%s'" highFreqTerms.[n % highFreqTerms.Count].Term 
                     medFreqTerms.[n % medFreqTerms.Count].Term)
            bqOrHighLowFile.WriteLine
                (sprintf "body = '%s' OR body = '%s'" highFreqTerms.[n % highFreqTerms.Count].Term 
                     lowFreqTerms.[n % lowFreqTerms.Count].Term)
            bqOrMedMedFile.WriteLine
                (sprintf "body = '%s' OR body = '%s'" medFreqTerms.[n % medFreqTerms.Count].Term 
                     medFreqTerms.[(n + 1) % medFreqTerms.Count].Term)
            bqOrMedLowFile.WriteLine
                (sprintf "body = '%s' OR body = '%s'" medFreqTerms.[n % medFreqTerms.Count].Term 
                     lowFreqTerms.[n % lowFreqTerms.Count].Term)
            bqOrLowLowFile.WriteLine
                (sprintf "body = '%s' OR body = '%s'" lowFreqTerms.[n % lowFreqTerms.Count].Term 
                     lowFreqTerms.[(n + 1) % lowFreqTerms.Count].Term)
    
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
        let file = new StreamReader(inputFile)
        let mutable proc = true
        let mutable i = 1
        while proc do
            let mutable line = file.ReadLine()
            if line = null then proc <- false
            else 
                let document = new Dictionary<string, string>()
                document.Add("title", line.Substring(0, line.IndexOf('|') - 1))
                document.Add("body", line.Substring(line.IndexOf('|') + 1))
                queueService.AddDocumentQueue(Global.WikiIndexName, (i.ToString()), document)
                i <- i + 1
                line <- file.ReadLine()
        indexService.Commit(Global.WikiIndexName) |> ignore
    
    //        let fileInfo = new FileInfo(inputFile)
    //        let fileSize = float (fileInfo.Length) / 1073741824.0
    //        let time = float (stopwatch.ElapsedMilliseconds) / 3600000.0
    //        let indexingSpeed = fileSize / time
    //        printfn "Total Records indexed: %i" i
    //        printfn "Total Elapsed time (ms): %i" stopwatch.ElapsedMilliseconds
    //        printfn "Total Data Size (MB): %f" (float (fileInfo.Length) / (1024.0 * 1024.0))
    //        printfn "Indexing Speed (GB/Hr): %f" indexingSpeed
    let PreCreateIndexingData(inputFile : string) = 
        let file = new StreamReader(inputFile)
        let mutable proc = true
        let mutable i = 1
        let result = new ResizeArray<WikiArticle>()
        while proc do
            let mutable line = file.ReadLine()
            if line = null then proc <- false
            else 
                let document = 
                    { Id = i.ToString()
                      Title = line.Substring(0, line.IndexOf('|') - 1)
                      Body = line.Substring(line.IndexOf('|') + 1) }
                i <- i + 1
                result.Add(document)
                line <- file.ReadLine()
        result.ToArray()
    
    let ExecuteIndexingTestThreadLocal(data : WikiArticle array) = 
        let localStore = 
            new ThreadLocal<Dictionary<string, string>>(fun _ -> 
            let dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            dict.Add("title", "")
            dict.Add("body", "")
            dict)
        
        let parallelOptions = new ParallelOptions(MaxDegreeOfParallelism = -1)
        let documentService = Global.DocumentService
        Parallel.ForEach(data, parallelOptions, 
                         (fun n -> 
                         try 
                             localStore.Value.["title"] <- n.Title
                             localStore.Value.["body"] <- n.Body
                             match documentService.AddDocument(Global.WikiIndexName, n.Id, localStore.Value) with
                             | Choice1Of2(a) -> ()
                             | Choice2Of2(e) -> printfn "Error: indexing document"
                         with e -> printfn "%A" e.Message
                         ()))
        |> ignore
        Global.IndexService.Commit(Global.WikiIndexName) |> ignore
    
    let ExecuteIndexingTestDataFlow(data : WikiArticle array) = 
        let queueService = Global.QueueService
        for article in data do
            let document = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            document.Add("title", article.Title)
            document.Add("body", article.Body)
            queueService.AddDocumentQueue(Global.WikiIndexName, article.Id, document)
        Global.IndexService.Commit(Global.WikiIndexName) |> ignore
    
    let WikipediaIndexingTest (inputFile : string) (useQueue : bool) = 
        let repeat = 10.0
        if File.Exists(inputFile) then 
            let data = PreCreateIndexingData inputFile
            
            let result = 
                if useQueue then Benchmark.Run((fun () -> ExecuteIndexingTestDataFlow data), (int) repeat, false)
                else Benchmark.Run((fun () -> ExecuteIndexingTestThreadLocal data), (int) repeat, false)
            printfn "%A" result
            let fileInfo = new FileInfo(inputFile)
            let fileSize = (float (fileInfo.Length) / 1073741824.0) * repeat
            let time = result.Elapsed.TotalHours
            let indexingSpeed = fileSize / time
            printfn "Records indexed per file: %i" (data.Count())
            printfn "Total Records indexed: %i" (data.Count() * (int) repeat)
            printfn "Total Elapsed time (ms): %f" result.Elapsed.TotalSeconds
            printfn "Total Data Size (MB): %f" fileSize
            printfn "Indexing Speed (GB/Hr): %f" indexingSpeed
        else failwithf "Cannot find the input file at the specified location: %s" inputFile
    
    /// <summary>
    /// Execute a given set of queries
    /// </summary>
    /// <param name="queries"></param>
    /// <param name="searchService"></param>
    /// <param name="threadCount"></param>
    let private ExecuteQuery (queries : string []) (searchService : ISearchService) (threadCount : int) = 
        let parallelOptions = new ParallelOptions(MaxDegreeOfParallelism = threadCount)
        Parallel.ForEach(queries, parallelOptions, 
                         (fun n -> 
                         try 
                             match searchService.Search(new SearchQuery(Global.WikiIndexName, n)) with
                             | Choice1Of2(a) -> ()
                             | Choice2Of2(e) -> printfn "Error: %s Query:%s" e.UserMessage n
                         with e -> printfn "%A" e.Message
                         ()))
        |> ignore
        ()
    
    /// <summary>
    /// Wikipedia queries tests
    /// </summary>
    /// <param name="folderPath"></param>
    let WikipediaQueryTests(folderPath : string) = 
        let testFiles = 
            [| "BooleanQueriesAndHighHigh"; "BooleanQueriesAndHighMed"; "BooleanQueriesAndHighLow"; 
               "BooleanQueriesAndMedMed"; "BooleanQueriesAndMedLow"; "BooleanQueriesAndLowLow"; 
               "BooleanQueriesOrHighHigh"; "BooleanQueriesOrHighMed"; "BooleanQueriesOrHighLow"; 
               "BooleanQueriesOrMedMed"; "BooleanQueriesOrMedLow"; "BooleanQueriesOrLowLow"; "Fuzzy1QueriesHigh"; 
               "Fuzzy1QueriesMed"; "Fuzzy1QueriesLow"; "Fuzzy2QueriesHigh"; "Fuzzy2QueriesMed"; "Fuzzy2QueriesLow"; 
               "TermQueriesHigh"; "TermQueriesMed"; "TermQueriesLow" |]
        let results = new ResizeArray<string * PerfResult>()
        let repeat = 3.0
        let totalQueriesPerFile = 1000.0
        
        let runTests (threadCount) = 
            for file in testFiles do
                let path = Path.Combine(folderPath, sprintf "%s.txt" file)
                if File.Exists(path) then 
                    let queries = System.IO.File.ReadAllLines(path)
                    let result = 
                        Benchmark.Run
                            ((fun () -> ExecuteQuery queries Global.SearchService threadCount), (int) repeat, true)
                    printfn "%A" result
                    results.Add((file, result))
        runTests 1
        runTests 2
        runTests 4
        runTests -1
        for (testFile, result) in results do
            let time = totalQueriesPerFile * repeat / (float) result.Elapsed.TotalSeconds
            printfn "%s : %f queries/second" testFile time
