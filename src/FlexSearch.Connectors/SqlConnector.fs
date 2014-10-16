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
    inherit HttpHandlerBase<unit, Guid>()
    
    // Generate a sample file for user
    let sampleFile = 
        let sampleData = new ConcurrentDictionary<string, SqlSetting>()
        let sqlSettings = new SqlSetting()
        sqlSettings.ConnectionString <- "connectionString"
        sqlSettings.Queries.TryAdd
            ("query1", new SqlQuery(SingleRequestQuery = "singleRequestQuery1", BulkRequestQuery = "bulkRequestQuery1")) 
        |> ignore
        sqlSettings.Queries.TryAdd
            ("query2", new SqlQuery(SingleRequestQuery = "singleRequestQuery2", BulkRequestQuery = "bulkRequestQuery2")) 
        |> ignore
        sampleData.TryAdd("SqlSettings1", sqlSettings) |> ignore
        sampleData.TryAdd("SqlSettings2", sqlSettings) |> ignore
        threadSafeWriter.WriteFile(Path.Combine(serverSettings.ConfFolder, "SqlConnector", "sample.yml"), sampleData)
    
    let settings = 
        match threadSafeWriter.ReadFile<ConcurrentDictionary<string, SqlSetting>>
                  (Path.Combine(serverSettings.ConfFolder, "SqlConnector", "sql.yml")) with
        | Choice1Of2(s) -> 
            let result = new ConcurrentDictionary<string, SqlSetting>(StringComparer.OrdinalIgnoreCase)
            for setting in s do
                let queries = new ConcurrentDictionary<string, SqlQuery>(StringComparer.OrdinalIgnoreCase)
                for sql in setting.Value.Queries do
                    queries.TryAdd(sql.Key, sql.Value) |> ignore
                setting.Value.Queries <- queries
                result.TryAdd(setting.Key, setting.Value) |> ignore
            result
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
                    let document = new FlexDocument(IndexName = indexName, Id = reader.[0].ToString())
                    for i = 1 to reader.FieldCount - 1 do
                        document.Fields.Add(reader.GetName(i), reader.GetValue(i).ToString())
                    if forceCreate then queueService.AddDocumentQueue(document)
                    else queueService.AddOrUpdateDocumentQueue(document)
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
    
    let bulkRequestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop = 
                async { 
                    let! (connectionString, query, indexName, jobId, forceCreate) = inbox.Receive()
                    ExecuteSql(connectionString, query, indexName, jobId, forceCreate)
                    return! loop
                }
            loop)
    
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
                    let jobId = Guid.NewGuid()
                    bulkRequestProcessor.Post
                        (c.ConnectionString, query.BulkRequestQuery, index, jobId.ToString(), forceCreate)
                    return! Choice1Of2(jobId)
                | id -> 
                    // Single index request
                    let! query = GetQuery(queryName, c)
                    let singleRequestQuery = query.SingleRequestQuery.Replace("{id}", id)
                    ExecuteSql(c.ConnectionString, singleRequestQuery, index, "", forceCreate)
                    return! Choice1Of2(Guid.Empty)
            | _ -> return! Choice2Of2("CONNECTION_NOT_FOUND:Connection does not exist." |> GenerateOperationMessage)
        }
    
    override this.Process(index, connectionName, body, context) = 
        ((ProcessRequest(index.Value, connectionName.Value, context)), Ok, BadRequest)
