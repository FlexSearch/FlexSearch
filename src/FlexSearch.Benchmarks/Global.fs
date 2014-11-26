namespace FlexSearch.Benchmarks

open Autofac
open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.TestSupport
open FlexSearch.Utility
open System

module Global = 
    /// <summary>
    ///  Global static container for Benchmarking
    /// </summary>
    let Container = IntegrationTestHelpers.Container
    
    let SearchService = Container.Resolve<ISearchService>()
    let IndexService = Container.Resolve<IIndexService>()
    let QueueService = Container.Resolve<IQueueService>()
    let DocumentService = Container.Resolve<IDocumentService>()
    let WikiIndexName = "wikipedia"
    
    let GetWikiIndex() = 
        let index = new Index()
        index.IndexName <- WikiIndexName
        index.Fields.Add(new Field("title", FieldType.Text, Store = false))
        index.Fields.Add(new Field("body", FieldType.Text, Store = false))
        index.IndexConfiguration.CommitTimeSeconds <- 500
        index.IndexConfiguration.RefreshTimeMilliseconds <- 500000
        index.IndexConfiguration.DirectoryType <- DirectoryType.MemoryMapped
        index.Online <- true
        index
    
    let AddIndex() = 
        match IndexService.AddIndex(GetWikiIndex()) with
        | Choice1Of2(_) -> ()
        | Choice2Of2(e) -> 
            if (Errors.INDEX_ALREADY_EXISTS |> GenerateOperationMessage).ErrorCode = e.ErrorCode then ()
            else failwithf "%A" e
