// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core.ImportHandlers

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
open System.Data
open System.Data.SqlClient
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Threading.Tasks.Dataflow

/// <summary>
/// Generic Importer module
/// </summary>
[<Name("importer")>]
type ImporterModule(importHandlerFactory : IFlexFactory<IImportHandler>, state : INodeState) = 
    inherit HttpModuleBase()
    let importHandlers = importHandlerFactory.GetAllModules()
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
    
    let processRequest (indexName, owin : IOwinContext) = 
        maybe { 
            let! importRequest = getRequestBody<ImportRequest> (owin.Request)
            match checkIdPresent (owin) with
            | Some(id) -> 
                match importHandlers.TryGetValue(id) with
                | (true, x) -> 
                    // Check if id is provided if yes then it is a single select query otherwise
                    // a multi-select query
                    match String.IsNullOrWhiteSpace(importRequest.Id) with
                    | false -> 
                        if x.SupportsBulkIndexing() then 
                            let jobId = Guid.NewGuid()
                            let job = new Job(jobId.ToString(), JobStatus.Initializing, Message = bulkIndexMessage)
                            state.PersistanceStore.Put (jobId.ToString()) job |> ignore
                            importRequest.JobId <- jobId.ToString()
                            await (requestQueue.SendAsync((indexName, jobId, owin.Request.Query)))
                            return! Choice1Of2(new ImportResponse(JobId = jobId.ToString(), Message = bulkIndexMessage))
                        else return! Choice2Of2(MessageConstants.IMPORTER_DOES_NOT_SUPPORT_BULK_INDEXING)
                    | true -> 
                        if x.SupportsIncrementalIndexing() then 
                            match x.ProcessIncrementalRequest(indexName, importRequest) with
                            | Choice1Of2(_) -> 
                                return! Choice1Of2(new ImportResponse(JobId = "", Message = incrementalIndexMessage))
                            | Choice2Of2(e) -> return! Choice2Of2(e)
                        else return! Choice2Of2(MessageConstants.IMPORTER_DOES_NOT_SUPPORT_INCREMENTAL_INDEXING)
                | _ -> return! Choice2Of2(MessageConstants.IMPORTER_NOT_FOUND)
            | None -> return! Choice2Of2(MessageConstants.IMPORTER_NOT_FOUND)
        }
    
    override this.Post(indexName, owin) = owin |> responseProcessor (processRequest (indexName, owin)) OK BAD_REQUEST

[<Name("sql")>]
type SqlImporter(queueService : IQueueService, state : INodeState) = 
    
    let sqlSettings = 
        let path = Path.Combine(Constants.ConfFolder, "Sql.json")
        if File.Exists(path) then 
            let fileText = Helpers.LoadFile(path)
            let parsedResult = JsonConvert.DeserializeObject<Dictionary<string, Connector.SqlSetting>>(fileText)
            parsedResult
        else new Dictionary<string, Connector.SqlSetting>()
    
    let getConnectionName (request : ImportRequest) = 
        match request.Parameters.TryGetValue("connectionName") with
        | (true, x) -> 
            match sqlSettings.TryGetValue(x) with
            | (true, y) -> Choice1Of2(y)
            | _ -> Choice2Of2(Connector.ConnectorConstants.CONNECTION_NAME_NOT_FOUND)
        | _ -> Choice2Of2(Connector.ConnectorConstants.CONNECTION_NAME_NOT_FOUND)
    
    let getQuery (request : ImportRequest, connectionName : Connector.SqlSetting) = 
        match request.Parameters.TryGetValue("query") with
        | (false, _) -> 
            // No query provided check for query name
            match request.Parameters.TryGetValue("queryName") with
            | (false, _) -> Choice2Of2(Connector.ConnectorConstants.QUERY_NAME_NOT_FOUND)
            | (true, x) -> 
                match connectionName.Queries.TryGetValue(x) with
                | (true, y) -> 
                    // Check if id is provided if yes then it is a single select query otherwise
                    // a multi-select query
                    match String.IsNullOrWhiteSpace(request.Id) with
                    | false -> Choice1Of2(y.MultipleSelectQuery)
                    | true -> Choice1Of2(y.SingleSelectQuery.Replace("id", request.Id))
                | _ -> Choice2Of2(Connector.ConnectorConstants.QUERY_NAME_NOT_FOUND)
        | (true, x) -> Choice1Of2(x)
    
    let executeSql (indexName, request : ImportRequest) = 
        match getConnectionName request with
        | Choice1Of2(connectionName) -> 
            match getQuery (request, connectionName) with
            | Choice1Of2(query) -> 
                try 
                    use connection = new SqlConnection(connectionName.ConnectionString)
                    use command = new SqlCommand(query, Connection = connection, CommandType = CommandType.Text)
                    command.CommandTimeout <- 300
                    connection.Open()
                    let mutable rows = 0
                    use reader = command.ExecuteReader()
                    if reader.HasRows = true then 
                        while reader.Read() do
                            let document = new Dictionary<string, string>()
                            document.Add(Constants.IdField, reader.[0].ToString())
                            for i = 1 to reader.FieldCount do
                                document.Add(reader.GetName(i), reader.GetValue(i).ToString())
                            if request.ForceCreate then 
                                queueService.AddDocumentQueue(indexName, (reader.[0].ToString()), document)
                            else queueService.AddOrUpdateDocumentQueue(indexName, (reader.[0].ToString()), document)
                            rows <- rows + 1
                            if String.IsNullOrWhiteSpace(request.JobId) <> true && rows % 5000 = 0 then 
                                let job = 
                                    new Job(request.JobId, JobStatus.InProgress, Message = "", ProcessedItems = rows)
                                state.PersistanceStore.Put request.JobId job |> ignore
                    else 
                        if String.IsNullOrWhiteSpace(request.JobId) <> true then 
                            let job = 
                                new Job(request.JobId, JobStatus.CompletedWithErrors, Message = "No rows returned.", 
                                        ProcessedItems = rows)
                            state.PersistanceStore.Put request.JobId job |> ignore
                        Logger.TraceErrorMessage(sprintf "SQL connector error. No rows returned. Query:{%s}" query)
                with e -> 
                    if String.IsNullOrWhiteSpace(request.JobId) <> true then 
                        let job = new Job(request.JobId, JobStatus.CompletedWithErrors, Message = e.Message)
                        state.PersistanceStore.Put request.JobId job |> ignore
                    Logger.TraceError("SQL connector error", e)
                Choice1Of2()
            | Choice2Of2(e) -> Choice2Of2(e)
        | Choice2Of2(e) -> Choice2Of2(e)
    
    interface IImportHandler with
        member this.SupportsBulkIndexing() = true
        member this.SupportsIncrementalIndexing() = true
        
        member this.ProcessBulkRequest(indexName, request) = 
            match executeSql (indexName, request) with
            | Choice1Of2() -> ()
            | Choice2Of2(e) -> Logger.TraceOperationMessageError("SQL connector error", e)
        
        member this.ProcessIncrementalRequest(indexName, request) = executeSql (indexName, request)
