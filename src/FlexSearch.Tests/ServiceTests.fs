module ServiceTests

open FlexSearch.Core
open Swensen.Unquote

module IndexServiceTests = 
    type AddIndexTests() = 
        member __.``Should add a new index`` (index : Index.Dto, indexService : IIndexService) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
        
        member __.``Newly created index should be online`` (indexService : IIndexService, index : Index.Dto) = 
            index.Online <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.GetIndexStatus(index.IndexName) = Choice1Of2(IndexState.OnlineMaster) @>
        
        member __.``Newly created index should be offline`` (indexService : IIndexService, index : Index.Dto) = 
            index.Online <- false
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.GetIndexStatus(index.IndexName) = Choice1Of2(IndexState.Offline) @>
        
        member __.``It is not possible to open an opened index`` (indexService : IIndexService, index : Index.Dto) = 
            index.Online <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.OpenIndex(index.IndexName) = Choice2Of2(IndexIsAlreadyOnline(index.IndexName)) @>
        
        member __.``It is not possible to close an closed index`` (indexService : IIndexService, index : Index.Dto) = 
            index.Online <- false
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.CloseIndex(index.IndexName) = Choice2Of2(IndexIsAlreadyOffline(index.IndexName)) @>
        
        member __.``Can not create the same index twice`` (indexService : IIndexService, index : Index.Dto) = 
            index.Online <- false
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.AddIndex(index) = Choice2Of2(IndexAlreadyExists(index.IndexName)) @>
        
        member __.``Offline index can be made online`` (indexService : IIndexService, index : Index.Dto) = 
            index.Online <- false
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
            test <@ indexService.GetIndexStatus(index.IndexName) = Choice1Of2(IndexState.OnlineMaster) @>
        
        member __.``Online index can be made offline`` (indexService : IIndexService, index : Index.Dto) = 
            index.Online <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
            test <@ indexService.GetIndexStatus(index.IndexName) = Choice1Of2(IndexState.Offline) @>
