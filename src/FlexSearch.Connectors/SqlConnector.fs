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
namespace FlexSearch.Connectors

open FlexSearch.Api
open FlexSearch.Api.Messages
open FlexSearch.Common
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Utility
open Microsoft.Owin
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.Data.SqlClient
open System.IO

[<Sealed>]
type SqlQuery() = 
    member val SingleRequestQuery = "" with get, set
    member val BulkRequestQuery = "" with get, set

[<Sealed>]
type SqlSetting() = 
    member val ConnectionString = "" with get, set
    member val Queries = new ConcurrentDictionary<string, SqlQuery>(StringComparer.OrdinalIgnoreCase) with get, set

[<Sealed>]
[<Name("POST-/indices/:id/sql/:id")>]
type SqlHandler(serverSettings : ServerSettings, queueService : IQueueService, jobService : IJobService, threadSafeWriter : IThreadSafeWriter, logger : ILogService) = 
    inherit HttpHandlerBase<unit, unit>()
    
    let settings = 
        match threadSafeWriter.ReadFile<ConcurrentDictionary<string, SqlSetting>>
                  (Path.Combine(serverSettings.ConfFolder, "sql.yml")) with
        | Choice1Of2(s) -> s
        | Choice2Of2(e) -> failwithf "%A" e
    
    let ExecuteSql(connectionString, query, indexName, jobId, forceCreate) = 
        try 
            use connection = new SqlConnection(connectionString)
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
                    if forceCreate then queueService.AddDocumentQueue(indexName, (reader.[0].ToString()), document)
                    else queueService.AddOrUpdateDocumentQueue(indexName, (reader.[0].ToString()), document)
                    rows <- rows + 1
                    if String.IsNullOrWhiteSpace(jobId) <> true && rows % 5000 = 0 then 
                        let job = 
                            new Job(JobId = jobId, Status = JobStatus.InProgress, Message = "", ProcessedItems = rows)
                        jobService.UpdateJob(job) |> ignore
                if String.IsNullOrWhiteSpace(jobId) <> true && rows % 5000 = 0 then 
                    let job = 
                        new Job(JobId = jobId, Status = JobStatus.Completed, Message = "Completed", 
                                ProcessedItems = rows)
                    jobService.UpdateJob(job) |> ignore
                    logger.TraceInformation("SQL connector", (jobService.ToString()))
            else 
                if String.IsNullOrWhiteSpace(jobId) <> true then 
                    let job = 
                        new Job(JobId = jobId, Status = JobStatus.CompletedWithErrors, Message = "No rows returned.", 
                                ProcessedItems = rows)
                    jobService.UpdateJob(job) |> ignore
                logger.TraceError(sprintf "SQL connector error. No rows returned. Query:{%s}" query)
        with e -> 
            if String.IsNullOrWhiteSpace(jobId) <> true then 
                let job = new Job(JobId = jobId, Status = JobStatus.CompletedWithErrors, Message = e.Message)
                jobService.UpdateJob(job) |> ignore
            logger.TraceError(sprintf "SQL connector error: %s" (ExceptionPrinter(e)))
    
    let GetQuery(queryName, sqlSettings : SqlSetting) = 
        match sqlSettings.Queries.TryGetValue(queryName) with
        | true, query -> Choice1Of2(query)
        | _ -> Choice2Of2("QUERY_NOT_FOUND:Query does not exist." |> GenerateOperationMessage)
    
    let ProcessRequest(index : string, connectionName : string, context : IOwinContext) = 
        maybe { 
            let queryName = GetValueFromQueryString "query" "default" context
            let forceCreate = GetBoolValueFromQueryString "forcecreate" false context
            match settings.TryGetValue(connectionName) with
            | true, c -> 
                match context.Request.Query.Get("id") with
                | null -> 
                    // Bulk request
                    let! query = GetQuery(queryName, c)
                    ExecuteSql
                        (c.ConnectionString, query.BulkRequestQuery, index, Guid.NewGuid().ToString(), forceCreate)
                | id -> 
                    // Single index request
                    let! query = GetQuery(queryName, c)
                    let singleRequestQuery = query.SingleRequestQuery.Replace("{id}", id)
                    ExecuteSql(c.ConnectionString, singleRequestQuery, index, "", forceCreate)
            | _ -> return! Choice2Of2("CONNECTION_NOT_FOUND:Connection does not exist." |> GenerateOperationMessage)
        }
    
    override this.Process(index, connectionName, body, context) = 
        ((ProcessRequest(index.Value, connectionName.Value, context)), Ok, BadRequest)
