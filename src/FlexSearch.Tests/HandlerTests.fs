module HandlerTests

open FlexSearch.Core
open Swensen.Unquote

module CsvHandlerTests = 
    open Microsoft.Owin

    type ImportCsvTests() = 
        member __.``Should index a CSV when file path is given`` (index : Index.Dto, queueService : IQueueService, 
                                                                   jobService : IJobService, 
                                                                   indexService : IIndexService) = 
            index.Online <- true
            let csvHandler = new CsvHandler(queueService, jobService)
            let csvReq = new CsvIndexingRequest(IndexName = index.IndexName, HasHeaderRecord = true, Path = "..\\..\\test.csv") |> Some
            let reqCntxt = RequestContext.Create(new OwinContext(), "", index.IndexName, defString, defString)
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ rSucceeded <| csvHandler.Process(reqCntxt, csvReq) @>
