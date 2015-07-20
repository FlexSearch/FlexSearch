module HandlerTests

open FlexSearch.Core
open Swensen.Unquote

module CsvHandlerTests = 
    open Microsoft.Owin
    open System.Diagnostics

    type ImportCsvTests() = 
        
        let generateCsvIndexJob queueService jobService (indexService : IIndexService) (index : Index.Index) = 
            let csvReq = 
                new CsvIndexingRequest(IndexName = index.IndexName, HasHeaderRecord = true, Path = "..\\..\\test.csv") 
                |> Some
            let reqCntxt = RequestContext.Create(new OwinContext(), defString, index.IndexName, defString, defString)
            let csvHandler = new CsvHandler(queueService, indexService, jobService)
            test <@ succeeded <| indexService.AddIndex(index) @>
            csvHandler.Process(reqCntxt, csvReq)
        
        member __.``Should create a CSV Indexing job when file path is given`` (index : Index.Index, 
                                                                                queueService : IQueueService, 
                                                                                jobService : IJobService, 
                                                                                indexService : IIndexService) = 
            index.Online <- true
            let jobResponse = index |> generateCsvIndexJob queueService jobService indexService
            test <@ rSucceeded <| jobResponse @>
        
        member __.``Should Change the job status to Completed when it's done`` (index : Index.Index, 
                                                                                queueService : IQueueService, 
                                                                                jobService : IJobService, 
                                                                                indexService : IIndexService) = 
            index.Online <- true
            let jobResponse = index |> generateCsvIndexJob queueService jobService indexService
            test <@ rSucceeded <| jobResponse @>

            let jobId = 
                match jobResponse with
                | SomeResponse(Choice1Of2(id),_,_) -> id
                | _ -> defString
            
            let sw = new Stopwatch()

            let rec loop() =
                let job = jobService.GetJob jobId
                test <@ succeeded job @>
                
                match job |> returnOrFail |> (fun j -> j.Status) with
                | JobStatus.Completed -> "ok"
                | status -> 
                    // if more than 35 seconds have passed since the job started, then assume it's a failure
                    if sw.ElapsedMilliseconds > 35000L then 
                        sw.Stop()
                        sprintf "More than 35 seconds have passed without the job completing. Job status: %A" status
                    else
                        loop()

            sw.Start()
            loop() =? "ok"
            

