namespace FlexSearch.Tests.Index

open FlexSearch.Tests
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

type CommitTests() = 
    
    member __.``Uncommitted changes can be recovered from TxLog in case of failure`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        // Add test document
        ih |> addDocByIdPass "1"
        ih |> addDocByIdPass "2"
        // Close the index without any commit
        ih |> closeIndexPass
        // Document should get recovered from TxLogs after index is reopened
        ih |> openIndexPass
        ih |> totalDocs 2
        ih |> getDocPass "1"
        ih |> getDocPass "2"
    
    member __.``Changes will be applied in the same order as receiveced 1`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        // Add test document
        ih |> addDocByIdPass "1"
        ih |> deleteDocByIdPass "1"
        // Close the index without any commit
        ih |> closeIndexPass
        // Document should get recovered from TxLogs after index is reopened
        ih |> openIndexPass
        ih |> totalDocs 0
        ih |> getDocsFail "1"
    
    member __.``Changes will be applied in the same order as receiveced 2`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> deleteDocByIdPass "1"
        ih |> addDocByIdPass "1"
        ih |> closeIndexPass
        // Document should get recovered from TxLogs after index is reopened
        ih |> openIndexPass
        ih |> totalDocs 1
        ih |> getDocPass "1"
        
    member __.``Older TxLog files are deleted immediately after a commit`` (index : Index, indexService : IIndexService, 
                                                                            documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        let writer = extract <| indexService.IsIndexOnline(index.IndexName)
        for i = 1 to 10 do
            test <@ succeeded <| documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) @>
            let beforeCommitTotalNoFiles = Directory.EnumerateFiles(writer.Settings.BaseFolder +/ "txlogs").Count()
            let olderTxFile = writer.Settings.BaseFolder +/ "txlogs" +/ writer.Generation.Value.ToString()
            test <@ File.Exists(olderTxFile) @>
            test <@ succeeded <| indexService.ForceCommit(index.IndexName) @>
            let afterCommitTotalNoFiles = Directory.EnumerateFiles(writer.Settings.BaseFolder +/ "txlogs").Count()
            test <@ afterCommitTotalNoFiles <= beforeCommitTotalNoFiles @>
