namespace FlexSearch.Benchmarks

open Autofac
open FlexSearch.Core

module Global = 
    ///  Global static container for Benchmarking
    let Container = Main.getContainer (ServerSettings.T.GetDefault(), true)
    
    let SearchService = Container.Resolve<ISearchService>()
    let IndexService = Container.Resolve<IIndexService>()
    let QueueService = Container.Resolve<IQueueService>()
    let DocumentService = Container.Resolve<IDocumentService>()
    let WikiIndexName = "wikipedia"
    
    let GetWikiIndex() = 
        let index = new Index.Dto()
        index.IndexName <- WikiIndexName
        index.Fields <- [| new Field.Dto("title", FieldType.Dto.Text, Store = false)
                           new Field.Dto("body", FieldType.Dto.Text, Store = false) |]
        index.IndexConfiguration.CommitTimeSeconds <- 500
        index.IndexConfiguration.RefreshTimeMilliseconds <- 500000
        index.IndexConfiguration.DirectoryType <- DirectoryType.Dto.MemoryMapped
        index.Online <- true
        index
    
    let AddIndex() = 
        match IndexService.IndexExists(WikiIndexName) with
        | true -> ()
        | _ -> 
            IndexService.AddIndex(GetWikiIndex())
            |> returnOrFail
            |> ignore
