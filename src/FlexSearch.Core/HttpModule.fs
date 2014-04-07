// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core.HttpModule

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Core.State
open FlexSearch.Utility
open Microsoft.Owin
open Newtonsoft.Json
open Owin
open System
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.Composition
open System.Linq
open System.Net
open System.Net.Http

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "index")>]
type IndexModule() = 
    inherit HttpModuleBase()
    override this.Get(indexName, owin, state) = 
        owin |> responseProcessor (state.IndexService.GetIndex(indexName)) OK BAD_REQUEST
    
    override this.Post(indexName, owin, state) = 
        match getRequestBody<Index> (owin.Request) with
        | Choice1Of2(index) -> 
            // Index name passed in URL takes precedence
            index.IndexName <- indexName
            owin |> responseProcessor (state.IndexService.AddIndex(index)) OK BAD_REQUEST
        | Choice2Of2(error) -> 
            if error.ErrorCode = 6002 then 
                // In case the error is no body defined then still try to create the index based on index name
                let index = new Index()
                index.IndexName <- indexName
                owin |> responseProcessor (state.IndexService.AddIndex(index)) OK BAD_REQUEST
            else owin |> BAD_REQUEST error
    
    override this.Delete(indexName, owin, state) = 
        owin |> responseProcessor (state.IndexService.DeleteIndex(indexName)) OK BAD_REQUEST
    override this.Put(indexName, owin, state) = 
        match getRequestBody<Index> (owin.Request) with
        | Choice1Of2(index) -> 
            // Index name passed in URL takes precedence
            index.IndexName <- indexName
            owin |> responseProcessor (state.IndexService.UpdateIndex(index)) OK BAD_REQUEST
        | Choice2Of2(error) -> owin |> BAD_REQUEST error

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "documents")>]
type DocumentModule() = 
    inherit HttpModuleBase()
    
    override this.Get(indexName, owin, state) = 
        let processRequest = 
            maybe { 
                match checkIdPresent (owin) with
                | Some(id) -> 
                    // documents/{id}
                    // Return the requested document
                    let q = new SearchQuery(indexName, (sprintf "%s = '%s'" Constants.IdField id))
                    q.ReturnScore <- false
                    q.ReturnFlatResult <- true
                    q.Columns.Add("*")
                    match state.IndexService.PerformQuery(indexName, q) with
                    | Choice1Of2(v') -> 
                        let result = v'.Documents.First().Fields
                        return! Choice1Of2(result :> obj)
                    | Choice2Of2(e) -> return! Choice2Of2(e)
                | None -> 
                    // documents
                    // Return top 10 documents
                    let q = new SearchQuery(indexName, (sprintf "%s matchall 'x'" Constants.IdField))
                    q.ReturnScore <- false
                    q.ReturnFlatResult <- true
                    q.Columns.Add("*")
                    q.MissingValueConfiguration.Add(Constants.IdField, MissingValueOption.Ignore)
                    match state.IndexService.PerformQuery(indexName, q) with
                    | Choice1Of2(v') -> 
                        if q.ReturnFlatResult then 
                            owin.Response.Headers.Add("RecordsReturned", [| v'.RecordsReturned.ToString() |])
                            owin.Response.Headers.Add("TotalAvailable", [| v'.TotalAvailable.ToString() |])
                            let result = v'.Documents |> Seq.map (fun x -> x.Fields)
                            return! Choice1Of2(result :> obj)
                        else return! Choice1Of2(v' :> obj)
                    | Choice2Of2(e) -> return! Choice2Of2(e)
            }
        owin |> responseProcessor processRequest OK BAD_REQUEST
    
    override this.Post(indexName, owin, state) = 
        let processRequest = 
            maybe { 
                match checkIdPresent (owin) with
                | Some(id) -> 
                    // documents/{id}
                    // Add the document by id
                    let! fields = getRequestBody<Dictionary<string, string>> (owin.Request)
                    match fields.TryGetValue(Constants.IdField) with
                    | true, _ -> 
                        // Override dictionary id with the URL id
                        fields.[Constants.IdField] <- id
                    | _ -> fields.Add(Constants.IdField, id)
                    return! state.IndexService.PerformCommand(indexName, IndexCommand.Create(id, fields))
                | None -> 
                    // documents
                    // Bulk addition of the documents
                    return! Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
            }
        owin |> responseProcessor processRequest OK BAD_REQUEST
    
    override this.Delete(indexName, owin, state) = 
        let processRequest = 
            maybe { 
                match checkIdPresent (owin) with
                | Some(id) -> 
                    // documents/{id}
                    // Delete the document by id
                    return! state.IndexService.PerformCommand(indexName, IndexCommand.Delete(id))
                | None -> 
                    // documents
                    // Bulk delete of the documents
                    return! Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
            }
        owin |> responseProcessor processRequest OK BAD_REQUEST
    
    override this.Put(indexName, owin, state) = 
        let processRequest = 
            maybe { 
                match checkIdPresent (owin) with
                | Some(id) -> 
                    // documents/{id}
                    // Add or update the document by id
                    let! fields = getRequestBody<Dictionary<string, string>> (owin.Request)
                    match fields.TryGetValue(Constants.IdField) with
                    | true, _ -> 
                        // Override dictionary id with the URL id
                        fields.[Constants.IdField] <- id
                    | _ -> fields.Add(Constants.IdField, id)
                    return! state.IndexService.PerformCommand(indexName, IndexCommand.Update(id, fields))
                | None -> 
                    // documents
                    // Bulk addition of the documents
                    return! Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
            }
        owin |> responseProcessor processRequest OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "search")>]
type SearchModule() = 
    inherit HttpModuleBase()
    
    let processRequest (indexName, owin : IOwinContext, state : NodeState) = 
        maybe { 
            let query = 
                match getRequestBody<SearchQuery> (owin.Request) with
                | Choice1Of2(q) -> q
                | Choice2Of2(_) -> 
                    // It is possible that the query is supplied through query-string
                    new SearchQuery()
            query.QueryString <- getValueFromQueryString "q" query.QueryString owin
            query.Columns <- match owin.Request.Query.Get("c") with
                             | null -> query.Columns
                             | v -> v.Split([| ',' |], System.StringSplitOptions.RemoveEmptyEntries).ToList()
            query.Count <- getIntValueFromQueryString "count" query.Count owin
            query.Skip <- getIntValueFromQueryString "skip" query.Skip owin
            query.OrderBy <- getValueFromQueryString "orderby" query.OrderBy owin
            query.ReturnFlatResult <- getBoolValueFromQueryString "returnflatresult" query.ReturnFlatResult owin
            query.IndexName <- indexName
            match state.IndexService.PerformQuery(indexName, query) with
            | Choice1Of2(v') -> 
                if query.ReturnFlatResult then 
                    owin.Response.Headers.Add("RecordsReturned", [| v'.RecordsReturned.ToString() |])
                    owin.Response.Headers.Add("TotalAvailable", [| v'.TotalAvailable.ToString() |])
                    let result = v'.Documents |> Seq.map (fun x -> x.Fields)
                    return! Choice1Of2(result :> obj)
                else return! Choice1Of2(v' :> obj)
            | Choice2Of2(e) -> return! Choice2Of2(e)
        }
    
    override this.Get(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
    override this.Post(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "exists")>]
type ExistsModule() = 
    inherit HttpModuleBase()
    
    let processRequest (indexName : string, owin : IOwinContext, state : NodeState) = 
        match state.IndexService.IndexStatus(indexName) with
        | Choice1Of2(_) -> Choice1Of2()
        | Choice2Of2(e) -> Choice2Of2(e)
    
    override this.Get(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
    override this.Post(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "status")>]
type StatusModule() = 
    inherit HttpModuleBase()
    
    override this.Get(indexName, owin, state) = 
        let processRequest = 
            match state.IndexService.IndexStatus(indexName) with
            | Choice1Of2(status) -> Choice1Of2(new IndexStatusResponse(status))
            | Choice2Of2(e) -> Choice2Of2(e)
        owin |> responseProcessor processRequest OK BAD_REQUEST
    
    override this.Post(indexName, owin, state) = 
        let processRequest = 
            match checkIdPresent (owin) with
            | Some(id) -> 
                match id with
                | InvariantEqual "online" -> state.IndexService.OpenIndex(indexName)
                | InvariantEqual "offline" -> state.IndexService.CloseIndex(indexName)
                | _ -> Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
            | None -> Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
        owin |> responseProcessor processRequest OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "analysis")>]
type AnalysisModule() = 
    inherit HttpModuleBase()
    let processRequest (indexName, owin, state : NodeState) = maybe { let! index = state.IndexService.GetIndex
                                                                                       (indexName)
                                                                      return! Choice1Of2() }
    override this.Get(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
    override this.Post(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "/")>]
type RootModule() = 
    inherit HttpModuleBase()
    override this.Get(indexName, owin, state) = 
        owin.Response.ContentType <- "text/html"
        owin.Response.StatusCode <- 200
        await 
            (owin.Response.WriteAsync
                 ("FlexSearch " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()))

open System.Data
open System.Data.SqlClient
open System.IO
open System.Threading.Tasks.Dataflow

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "importer")>]
type ImporterModule() = 
    inherit HttpModuleBase()
    let importHandlers = GetImportHandlerModules().Value
    let incrementalIndexMessage = "Incremental index request completed."
    let bulkIndexMessage = 
        "Bulk-index request submitted to the importer module. Please use the provided jobId to query the job status."
    let processQueueItem (indexName : string, jobId : System.Guid, parameters : IReadableStringCollection) = ()
    
    let requestQueue = 
        let executionBlockOption = new ExecutionDataflowBlockOptions()
        executionBlockOption.MaxDegreeOfParallelism <- -1
        executionBlockOption.BoundedCapacity <- 10
        let queue = new ActionBlock<string * Guid * IReadableStringCollection>(processQueueItem, executionBlockOption)
        queue
    
    let processRequest (indexName, owin, state : NodeState) = 
        maybe { 
            match checkIdPresent (owin) with
            | Some(id) -> 
                match importHandlers.TryGetValue(id) with
                | (true, x) -> 
                    // Check if id is provided if yes then it is a single select query otherwise
                    // a multi-select query
                    match owin.Request.Query.Get("id") with
                    | null -> 
                        if x.SupportsBulkIndexing() then 
                            let jobId = Guid.NewGuid()
                            let job = new Job(jobId.ToString(), JobStatus.Initializing, Message = bulkIndexMessage)
                            state.PersistanceStore.Put (jobId.ToString()) job |> ignore
                            await (requestQueue.SendAsync((indexName, jobId, owin.Request.Query)))
                            return! Choice1Of2
                                        (new ImporterResponse(JobId = jobId.ToString(), Message = bulkIndexMessage))
                        else return! Choice2Of2(MessageConstants.IMPORTER_DOES_NOT_SUPPORT_BULK_INDEXING)
                    | y -> 
                        if x.SupportsIncrementalIndexing() then 
                            return! x.ProcessIncrementalRequest(indexName, y, owin.Request.Query)
                        else return! Choice2Of2(MessageConstants.IMPORTER_DOES_NOT_SUPPORT_INCREMENTAL_INDEXING)
                | _ -> return! Choice2Of2(MessageConstants.IMPORTER_NOT_FOUND)
            | None -> return! Choice2Of2(MessageConstants.IMPORTER_NOT_FOUND)
        }
    
    override this.Post(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
