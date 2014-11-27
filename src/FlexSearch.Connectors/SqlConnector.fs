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
open FlexSearch.Api.Validation
open System.ComponentModel.DataAnnotations

/// <summary>
/// Represents a request which can be sent to Sql connector to index SQL data
/// </summary>
[<Sealed>]
type SqlIndexingRequest() = 
    inherit ValidatableObjectBase<SqlIndexingRequest>()
    
    /// <summary>
    /// Name of the index
    /// </summary>
    [<Required>]
    member val IndexName = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// The query to be used to fetch data from Sql server
    /// </summary>
    [<Required>]
    member val Query = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// Connection string used to connect to the server
    /// </summary>
    [<Required>]
    member val ConnectionString = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// Signifies if all updates to the index are create
    /// </summary>
    [<Required>]
    member val ForceCreate = true with get, set
    
    /// <summary>
    /// Signifies if the connector should create a job for the task and return a jobId which can be used
    /// to check the status of the job.
    /// </summary>
    member val CreateJob = false with get, set

[<Sealed>]
[<Name("POST-/indices/:id/sql")>]
type SqlHandler(serverSettings : ServerSettings, queueService : IQueueService, jobService : IJobService, threadSafeWriter : IThreadSafeWriter, logger : ILogService) = 
    inherit HttpHandlerBase<SqlIndexingRequest, string>()
    
    let ExecuteSql(request : SqlIndexingRequest, jobId) = 
        if request.CreateJob then 
            let job = new Job(JobId = jobId, Status = JobStatus.InProgress)
            jobService.UpdateJob(job) |> ignore
        try 
            use connection = new SqlConnection(request.ConnectionString)
            use command = new SqlCommand(request.Query, Connection = connection, CommandType = CommandType.Text)
            command.CommandTimeout <- 300
            connection.Open()
            let mutable rows = 0
            use reader = command.ExecuteReader()
            if reader.HasRows = true then 
                while reader.Read() do
                    let document = new FlexDocument(IndexName = request.IndexName, Id = reader.[0].ToString())
                    for i = 1 to reader.FieldCount - 1 do
                        document.Fields.Add(reader.GetName(i), reader.GetValue(i).ToString())
                    if request.ForceCreate then queueService.AddDocumentQueue(document)
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
                logger.TraceError(sprintf "SQL connector error. No rows returned. Query:{%s}" request.Query)
        with e -> 
            if String.IsNullOrWhiteSpace(jobId) <> true then 
                let job = new Job(JobId = jobId, Status = JobStatus.CompletedWithErrors, Message = e.Message)
                jobService.UpdateJob(job) |> ignore
            logger.TraceError(sprintf "SQL connector error: %s" (ExceptionPrinter(e)))
    
    let bulkRequestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (body, jobId) = inbox.Receive()
                    ExecuteSql(body, jobId)
                    return! loop()
                }
            loop())
    
    let ProcessRequest(index : string, body : SqlIndexingRequest, context : IOwinContext) = 
        maybe { 
            body.IndexName <- index
            do! (body :> IValidator).MaybeValidator()
            let jobId = 
                if body.CreateJob then 
                    let guid = Guid.NewGuid()
                    bulkRequestProcessor.Post(body, guid.ToString())
                    guid.ToString()
                else 
                    ExecuteSql(body, "")
                    ""
            return! Choice1Of2(jobId)
        }
    
    override this.Process(index, connectionName, body, context) = 
        (ProcessRequest(index.Value, body.Value, context), Ok, BadRequest)
