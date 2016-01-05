// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

module WikipediaPerformanceTests = 
    open FlexSearch.Api.Model
    open System.IO
    open PerfUtil
    open System.Linq
    open System.Threading.Tasks
    
    let QueriesCount = 1000
    let WikiIndexName = "wikipedia"
    
    /// Wikipedia article details
    type WikiArticle = 
        { Id : string
          Title : string
          Body : string }
    
    /// Wikipedia term frequency information
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
    
    /// Generates random queries from Wikipedia 4KB corpus
    let RandomQueryGenerator (outputFolder : string) (indexService : IIndexService) = 
        let searchers = 
            match indexService.GetRealtimeSearchers(WikiIndexName) with
            | Ok(s) -> s
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
            FlexLucene.Misc.HighFreqTerms.GetHighFreqTerms
                (searchers.[0].IndexReader, 10000, "body[lucene_4_9]<lucene_4_1>", 
                 new FlexLucene.Misc.HighFreqTermsDocFreqComparator())
        let medFreqUpperRange = 1000000
        let medFreqLowerRange = 100000
        let lowFreqLowerRange = 10000
        // Divide the terms in 3 ranges
        for freqTerm in termFreq do
            let term = freqTerm.Termtext.Utf8ToString().Replace("'", "\\'")
            match freqTerm.DocFreq with
            | x when x >= medFreqUpperRange -> 
                highFreqTerms.Add({ Term = term
                                    DocFrequency = freqTerm.DocFreq
                                    TermFrequency = freqTerm.TotalTermFreq })
            | x when x >= medFreqLowerRange && x < medFreqUpperRange -> 
                medFreqTerms.Add({ Term = term
                                   DocFrequency = freqTerm.DocFreq
                                   TermFrequency = freqTerm.TotalTermFreq })
            | x when x >= lowFreqLowerRange && x < medFreqLowerRange -> 
                lowFreqTerms.Add({ Term = term
                                   DocFrequency = freqTerm.DocFreq
                                   TermFrequency = freqTerm.TotalTermFreq })
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
                let document = new Document(IndexName = WikiIndexName, Id = (i.ToString()))
                document.Fields.Add("title", line.Substring(0, line.IndexOf('|') - 1))
                document.Fields.Add("body", line.Substring(line.IndexOf('|') + 1))
                queueService.AddDocumentQueue(document)
                i <- i + 1
                line <- file.ReadLine()
        indexService.Commit(WikiIndexName) |> ignore
    
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
    
    let ExecuteIndexingTestDataFlow (data : WikiArticle array) (queueService : IQueueService) 
        (indexService : IIndexService) = 
        for article in data do
            let document = new Document(IndexName = WikiIndexName, Id = article.Id)
            document.Fields.Add("title", article.Title)
            document.Fields.Add("body", article.Body)
            queueService.AddDocumentQueue(document)
        indexService.Commit(WikiIndexName) |> ignore
    
    let WikipediaIndexingTest (inputFile : string) (queueService : IQueueService) (indexService : IIndexService) = 
        let repeat = 10.0
        if File.Exists(inputFile) then 
            let data = PreCreateIndexingData inputFile
            let result = 
                Benchmark.Run
                    ((fun () -> ExecuteIndexingTestDataFlow data queueService indexService), (int) repeat, false)
            printfn "%A" result
            let fileInfo = new FileInfo(inputFile)
            let fileSize = (float (fileInfo.Length) / 1073741824.0) * repeat
            let time = result.Elapsed.TotalHours
            let indexingSpeed = fileSize / time
            printfn "Records indexed per file: %i" (data.Count())
            printfn "Total Records indexed: %i" (data.Count() * (int) repeat)
            printfn "Total Elapsed time (ms): %f" result.Elapsed.TotalSeconds
            printfn "Total Data Size (GB): %f" fileSize
            printfn "Indexing Speed (GB/Hr): %f" indexingSpeed
        else failwithf "Cannot find the input file at the specified location: %s" inputFile
    
    /// Execute a given set of queries
    let private ExecuteQuery (queries : string []) (searchService : ISearchService) (threadCount : int) = 
        let parallelOptions = new ParallelOptions(MaxDegreeOfParallelism = threadCount)
        Parallel.ForEach(queries, parallelOptions, 
                         (fun n -> 
                         try 
                             match searchService.Search(new SearchQuery(WikiIndexName, n)) with
                             | Ok(_) -> ()
                             | Fail(e) -> printfn "Error: %A Query:%s" e n
                         with e -> printfn "%A" e.Message
                         ()))
        |> ignore
        ()
    
    /// Wikipedia queries tests
    let WikipediaQueryTests (folderPath : string) (longTest : bool) (searchService : ISearchService) = 
        let testFiles = 
            [| "BooleanQueriesAndHighHigh"; "BooleanQueriesAndHighMed"; "BooleanQueriesAndHighLow"; 
               "BooleanQueriesAndMedMed"; "BooleanQueriesAndMedLow"; "BooleanQueriesAndLowLow"; 
               "BooleanQueriesOrHighHigh"; "BooleanQueriesOrHighMed"; "BooleanQueriesOrHighLow"; 
               "BooleanQueriesOrMedMed"; "BooleanQueriesOrMedLow"; "BooleanQueriesOrLowLow"; "Fuzzy1QueriesHigh"; 
               "Fuzzy1QueriesMed"; "Fuzzy1QueriesLow"; "Fuzzy2QueriesHigh"; "Fuzzy2QueriesMed"; "Fuzzy2QueriesLow"; 
               "TermQueriesHigh"; "TermQueriesMed"; "TermQueriesLow" |]
        let results = new ResizeArray<string * int * PerfResult>()
        let repeat = 3.0
        let totalQueriesPerFile = 1000.0
        
        let runTests (threadCount) = 
            printfn "Starting test for thread count: %i" threadCount
            let mutable i = 1
            for file in testFiles do
                printfn "Executing test %i\%i File: %s Thread Count:%i" i (testFiles.Count()) file threadCount
                let path = Path.Combine(folderPath, sprintf "%s.txt" file)
                if File.Exists(path) then 
                    let queries = System.IO.File.ReadAllLines(path)
                    let result = 
                        Benchmark.Run((fun () -> ExecuteQuery queries searchService threadCount), (int) repeat, true)
                    printfn "%A" result
                    i <- i + 1
                    results.Add((file, threadCount, result))
        if longTest then 
            runTests 1
            runTests 2
            runTests 4
            runTests -1
        else runTests -1
        let fs = 
            new FileStream(Path.Combine
                               (folderPath, 
                                sprintf "QueryTestsResult-%s.txt" (System.DateTime.Now.ToString("yyyyMMddHHmm"))), 
                           FileMode.Create)
        printfn "Long Test: %b" longTest
        printfn "Summary"
        for (testFile, threadCount, result) in results do
            let time = totalQueriesPerFile * repeat / (float) result.Elapsed.TotalSeconds
            printfn "%s (Threads:%i) : %f queries/second" testFile threadCount time
        printfn "Details"
        for (testFile, threadCount, result) in results do
            let time = totalQueriesPerFile * repeat / (float) result.Elapsed.TotalSeconds
            printfn "%s (Threads:%i) : %f queries/second" testFile threadCount time
            printfn "%A" result
