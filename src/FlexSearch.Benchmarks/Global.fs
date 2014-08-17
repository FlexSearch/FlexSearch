namespace FlexSearch.Benchmarks

open FlexSearch.TestSupport
open Autofac
open FlexSearch.Core
open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
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
        index.Fields.Add("title", new FieldProperties(FieldType = FieldType.Text, Store = false))
        index.Fields.Add("body", new FieldProperties(FieldType = FieldType.Text, Store = false))
        index.IndexConfiguration.CommitTimeSec <- 500
        index.IndexConfiguration.RefreshTimeMilliSec <- 500000
        index.IndexConfiguration.DirectoryType <- DirectoryType.MemoryMapped
        index.Online <- true
        index
    
    do IndexService.AddIndex(GetWikiIndex()) |> ignore
       System.Threading.Thread.Sleep(1000)
