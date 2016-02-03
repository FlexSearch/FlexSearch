module IndexTests

open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open FlexSearch.Core
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Kernel
open Swensen.Unquote
open System.IO
open System.Linq
open System.Threading
open System.Collections.Generic
open FsCheck

type TransactionWriterTests() = 
    
    let getTransactionLogEntry() = 
        let fixture = new Ploeh.AutoFixture.Fixture()
        let context = new SpecimenContext(fixture)
        let txEntry = context.Create<TransactionEntry>()
        txEntry
    
    member __.``Transaction can be added and retrieved``() = 
        let writer = new TxWriter(0L)
        let entries = Array.create 5 (getTransactionLogEntry())
        entries |> Array.iter (fun entry -> writer.AppendEntry(entry, 0L))
        let result = writer.ReadLog(0L) |> Seq.toArray
        test <@ result.Length = 5 @>
        test <@ result.[0].Id = entries.[0].Id @>
        test <@ result.[1].Id = entries.[1].Id @>
        test <@ result.[2].Id = entries.[2].Id @>
        test <@ result.[3].Id = entries.[3].Id @>
        test <@ result.[4].Id = entries.[4].Id @>

    member __.``Random Log read write test using FSCheck``() =
        let writer = new TxWriter(0L)
        let entries = Gen.listOf Arb.generate<TransactionEntry> |> Gen.eval 1000 (Random.StdGen(20000, 100))
        entries |> List.iter (fun entry -> writer.AppendEntry(entry, 0L))
        let result = writer.ReadLog(0L) |> Seq.toList
        test <@ entries.Length = result.Length @>
        for i = 0 to entries.Length - 1 do
            let entryId = entries.[i].Id
            let resultId = result.[i].Id
            test <@ entryId = resultId @>
            test <@ entries.[i].ModifyIndex = result.[i].ModifyIndex @>
            test <@ entries.[i].Operation = result.[i].Operation @>
            let entryKeys = entries.[i].Data.Keys.ToArray()
            let resultKeys = result.[i].Data.Keys.ToArray()
            test <@ entryKeys = resultKeys @>
            let entryValues = entries.[i].Data.Values.ToArray()
            let resultValues = result.[i].Data.Values.ToArray()
            test <@ entryValues = resultValues @>

type CommitTests() = 
    member __.``Uncommitted changes can be recovered from TxLog in case of failure`` (index : Index, 
                                                                                      indexService : IIndexService, 
                                                                                      documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        // Add test document
        test <@ succeeded <| documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) @>
        test <@ succeeded <| documentService.AddDocument(new Document(indexName = index.IndexName, id = "2")) @>
        // Close the index without any commit
        test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
        // Document should get recovered from TxLogs after index is reopened
        test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
        test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 2 @>
        test <@ succeeded <| documentService.GetDocument(index.IndexName, "1") @>
        test <@ succeeded <| documentService.GetDocument(index.IndexName, "2") @>

    member __.``Changes will be applied in the same order as receiveced 1`` (index : Index, 
                                                                             indexService : IIndexService, 
                                                                             documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        // Add test document
        test <@ succeeded <| documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) @>
        test <@ succeeded <| documentService.DeleteDocument(index.IndexName, "1") @>
        // Close the index without any commit
        test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
        // Document should get recovered from TxLogs after index is reopened
        test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
        test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 0 @>
        test <@ failed <| documentService.GetDocument(index.IndexName, "1") @>

    member __.``Changes will be applied in the same order as receiveced 2`` (index : Index, 
                                                                             indexService : IIndexService, 
                                                                             documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        // Add test document
        test <@ succeeded <| documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) @>
        test <@ succeeded <| documentService.DeleteDocument(index.IndexName, "1") @>
        test <@ succeeded <| documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) @>
        // Close the index without any commit
        test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
        // Document should get recovered from TxLogs after index is reopened
        test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
        test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 1 @>
        test <@ succeeded <| documentService.GetDocument(index.IndexName, "1") @>

    member __.``TxLog file is changed immediately after a commit``( index : Index, 
                                                                    indexService : IIndexService, 
                                                                    documentService : IDocumentService) = 
        // Reduce max buffered docs count so that we can flush quicker
        index.IndexConfiguration.MaxBufferedDocs <- 3
        let random = new System.Random()
        let commitCount = random.Next(5, 20)
        let documentsPerCommit = random.Next(1, 10)
         
        test <@ succeeded <| indexService.AddIndex(index) @>
        let writer = extract <| indexService.IsIndexOnline(index.IndexName)
        
        for i = 1 to commitCount do
            test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = (i - 1) * documentsPerCommit @>
            let previousGen  = writer.ShardWriters.[0].Generation.Value
            // Do a commit before hand so that we can see the tx file change
            test <@ succeeded <| indexService.ForceCommit(index.IndexName) @>
            // Commit should cause a flush
            // TODO: Fix this
            // test <@ writer.ShardWriters.[0].OutstandingFlushes.Value = 1L @>
            // New generation must be 1 higer than the last
            test <@ writer.ShardWriters.[0].Generation.Value = previousGen + 1L  @>
            
            for j = 1 to documentsPerCommit do 
                test <@ succeeded <| documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) @>
            let txFile = writer.ShardWriters.[0].TxLogPath +/ writer.ShardWriters.[0].Generation.Value.ToString()
            // Test if the TxLog file is present with the current generation
            test <@ File.Exists(txFile) @>

    member __.``Older TxLog files are deleted immediately after a commit``( index : Index, 
                                                                            indexService : IIndexService, 
                                                                            documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        let writer = extract <| indexService.IsIndexOnline(index.IndexName)
        for i = 1 to 10 do
            test <@ succeeded <| documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) @>
            let beforeCommitTotalNoFiles = Directory.EnumerateFiles(writer.ShardWriters.[0].TxLogPath).Count()
            let olderTxFile = writer.ShardWriters.[0].TxLogPath +/ writer.ShardWriters.[0].Generation.Value.ToString()
            test <@ File.Exists(olderTxFile) @>
            test <@ succeeded <| indexService.ForceCommit(index.IndexName) @>
            let afterCommitTotalNoFiles = Directory.EnumerateFiles(writer.ShardWriters.[0].TxLogPath).Count()
            test <@ afterCommitTotalNoFiles <= beforeCommitTotalNoFiles @>
