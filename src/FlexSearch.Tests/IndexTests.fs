module IndexTests

open FlexSearch.Core
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Kernel
open Swensen.Unquote
open System.IO
open System.Linq
open System.Threading

type TransactionWriterTests() = 
    
    let getTransactionLogEntry() = 
        let fixture = new Ploeh.AutoFixture.Fixture()
        use stream = new MemoryStream()
        let context = new SpecimenContext(fixture)
        let txEntry = context.Create<TransactionLog.T>()
        TransactionLog.serializer (stream, txEntry)
        (txEntry, stream.ToArray())
    
    member __.``Transaction can be added and retrieved``() = 
        if File.Exists(DataHelpers.rootFolder +/ "0") then File.Delete(DataHelpers.rootFolder +/ "0")
        let writer = new TransactionLog.TxWriter(DataHelpers.rootFolder, 0L)
        let entries = Array.create 5 (getTransactionLogEntry())
        entries |> Array.iter (fun entry -> writer.Append(snd entry, 0L))
        let txEntries = entries |> Array.map (fun entry -> fst entry)
        // TODO: Will have to find a cleaner way than using Thread.Sleep
        // Maybe convert from MailboxProcessor to ActionBlock
        Thread.Sleep(5000)
        let result = writer.ReadLog(0L) |> Seq.toArray
        test <@ result.Length = 5 @>
        test <@ result.[0].Id = txEntries.[0].Id @>
        test <@ result.[1].Id = txEntries.[1].Id @>
        test <@ result.[2].Id = txEntries.[2].Id @>
        test <@ result.[3].Id = txEntries.[3].Id @>
        test <@ result.[4].Id = txEntries.[4].Id @>

type CommitTests() = 
    member __.``Uncommitted changes can be recovered from TxLog in case of failure`` (index : Index.Dto, 
                                                                                      indexService : IIndexService, 
                                                                                      documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        // Add test document
        test <@ succeeded <| documentService.AddDocument(new Document.Dto(index.IndexName, "1")) @>
        test <@ succeeded <| documentService.AddDocument(new Document.Dto(index.IndexName, "2")) @>
        // Close the index without any commit
        test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
        // Document should get recovered from TxLogs after index is reopened
        test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
        test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 2 @>
        test <@ succeeded <| documentService.GetDocument(index.IndexName, "1") @>
        test <@ succeeded <| documentService.GetDocument(index.IndexName, "2") @>

    member __.``Changes will be applied in the same order as receiveced 1`` (index : Index.Dto, 
                                                                             indexService : IIndexService, 
                                                                             documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        // Add test document
        test <@ succeeded <| documentService.AddDocument(new Document.Dto(index.IndexName, "1")) @>
        test <@ succeeded <| documentService.DeleteDocument(index.IndexName, "1") @>
        // Close the index without any commit
        test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
        // Document should get recovered from TxLogs after index is reopened
        test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
        test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 0 @>
        test <@ failed <| documentService.GetDocument(index.IndexName, "1") @>

    member __.``Changes will be applied in the same order as receiveced 2`` (index : Index.Dto, 
                                                                             indexService : IIndexService, 
                                                                             documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        // Add test document
        test <@ succeeded <| documentService.AddDocument(new Document.Dto(index.IndexName, "1")) @>
        test <@ succeeded <| documentService.DeleteDocument(index.IndexName, "1") @>
        test <@ succeeded <| documentService.AddDocument(new Document.Dto(index.IndexName, "1")) @>
        // Close the index without any commit
        test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
        // Document should get recovered from TxLogs after index is reopened
        test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
        test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 1 @>
        test <@ succeeded <| documentService.GetDocument(index.IndexName, "1") @>

    member __.``TxLog file is changed immediately after a commit``( index : Index.Dto, 
                                                                    indexService : IIndexService, 
                                                                    documentService : IDocumentService) = 
        // Reduce max buffered docs count so that we can flush quicker
        index.IndexConfiguration.MaxBufferedDocs <- 2
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
                test <@ succeeded <| documentService.AddDocument(new Document.Dto(index.IndexName, "1")) @>
            let txFile = writer.ShardWriters.[0].TxLogPath +/ writer.ShardWriters.[0].Generation.Value.ToString()
            // Test if the TxLog file is present with the current generation
            test <@ File.Exists(txFile) @>

    member __.``Older TxLog files are deleted immediately after a commit``( index : Index.Dto, 
                                                                            indexService : IIndexService, 
                                                                            documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        let writer = extract <| indexService.IsIndexOnline(index.IndexName)
        for i = 1 to 10 do
            test <@ succeeded <| documentService.AddDocument(new Document.Dto(index.IndexName, "1")) @>
            let beforeCommitTotalNoFiles = Directory.EnumerateFiles(writer.ShardWriters.[0].TxLogPath).Count()
            let olderTxFile = writer.ShardWriters.[0].TxLogPath +/ writer.ShardWriters.[0].Generation.Value.ToString()
            test <@ File.Exists(olderTxFile) @>
            test <@ succeeded <| indexService.ForceCommit(index.IndexName) @>
            let afterCommitTotalNoFiles = Directory.EnumerateFiles(writer.ShardWriters.[0].TxLogPath).Count()
            test <@ afterCommitTotalNoFiles <= beforeCommitTotalNoFiles @>
