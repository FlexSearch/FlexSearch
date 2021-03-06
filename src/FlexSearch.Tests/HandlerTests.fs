﻿module HandlerTests

open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open FlexSearch.Core
open Swensen.Unquote

module CsvHandlerTests = 
    open Microsoft.AspNetCore.Http
    open System.Diagnostics

    type ImportCsvTests() = 
        
        let generateCsvIndexJob queueService jobService (indexService : IIndexService) (index : Index) = 
            let csvReq = 
                new CsvIndexingRequest(IndexName = index.IndexName, HasHeaderRecord = true, Path = Constants.rootFolder +/ "test.csv") 
                |> Some
            let reqCntxt = RequestContext.Create(null, defString, index.IndexName, defString, defString)
            let csvHandler = new CsvHandler(queueService, indexService, jobService)
            test <@ succeeded <| indexService.AddIndex(index) @>
            csvHandler.Process(reqCntxt, csvReq)
        
        member __.``Should create a CSV Indexing job when file path is given`` (index : Index, 
                                                                                queueService : IQueueService, 
                                                                                jobService : IJobService, 
                                                                                indexService : IIndexService) = 
            index.Active <- true
            let jobResponse = index |> generateCsvIndexJob queueService jobService indexService
            test <@ rSucceeded <| jobResponse @>
        
        member __.``Should Change the job status to Completed when it's done`` (index : Index, 
                                                                                queueService : IQueueService, 
                                                                                jobService : IJobService, 
                                                                                indexService : IIndexService) = 
            index.Active <- true
            let jobResponse = index |> generateCsvIndexJob queueService jobService indexService
            test <@ rSucceeded <| jobResponse @>

            let jobId = 
                match jobResponse with
                | SomeResponse(Ok(id),_,_) -> id
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
            

