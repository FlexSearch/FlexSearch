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
open CsvHelper
open CsvHelper.Configuration
open System
open FlexSearch.Api.Validation
open System.ComponentModel.DataAnnotations
open System.ComponentModel

/// <summary>
/// Represents a request which can be sent to CSV connector to index CSV data.
/// </summary>
[<Sealed>]
type CsvIndexingRequest() = 
    inherit ValidatableObjectBase<CsvIndexingRequest>()
    
    /// <summary>
    /// Name of the index
    /// </summary>
    [<Required>]
    member val IndexName = Unchecked.defaultof<_> with get, set
    
    /// <summary>
    /// Signifies if the passed CSV file(s) has a header record 
    /// </summary>
    [<DefaultValue(false)>]
    member val HasHeaderRecord = false with get, set
    
    /// <summary>
    /// The headers to be used by each column. This should only be passed when there is
    /// no header in the csv file. The first column is always assumed to be id field. Make sure
    /// in your array you always offset the column names by 1 position.
    /// </summary>
    member val Headers = Unchecked.defaultof<string array> with get, set
    
    /// <summary>
    /// The path of the folder or file to be indexed. The service will pickup all files with 
    /// .csv extension.
    /// </summary>
    [<Required>]
    member val Path = Unchecked.defaultof<string> with get, set

/// <summary>
/// Connector for importing CSV file data into the system.
/// </summary>
[<Sealed>]
[<Name("POST-/indices/:id/csv")>]
type CsvHandler(queueService : IQueueService, jobService : IJobService, logger : ILogService) = 
    inherit HttpHandlerBase<CsvIndexingRequest, string>()
    
    let ProcessFile(body : CsvIndexingRequest, path : string) = 
        let configuration = new CsvConfiguration()
        configuration.HasHeaderRecord <- body.HasHeaderRecord
        use textReader = File.OpenText(path)
        let parser = new CsvParser(textReader, configuration)
        let reader = new CsvReader(parser)
        let headers = 
            if body.HasHeaderRecord then
                if reader.Read()  then
                    reader.FieldHeaders    
                else Array.zeroCreate(0)
            else
                body.Headers
        if headers.Length <> 0 then
            if reader.Read() then 
                while reader.Read() do
                    let document = new FlexDocument(body.IndexName, reader.CurrentRecord.[0])
                    // The first column is always id
                    for i = 1 to headers.Length - 1 do
                        document.Fields.Add(headers.[i], reader.CurrentRecord.[i])
                    queueService.AddDocumentQueue(document)
    
    let bulkRequestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (body : CsvIndexingRequest, path : string, jobId : Guid, isFolder : bool) = inbox.Receive()
                    let job = new Job(JobId = jobId.ToString("N"), Status = JobStatus.InProgress)
                    jobService.UpdateJob(job) |> ignore
                    if isFolder then 
                        for file in Directory.EnumerateFiles(path) do
                            ProcessFile(body, file)
                            job.ProcessedItems <- job.ProcessedItems + 1
                            jobService.UpdateJob(job) |> ignore
                    else 
                        ProcessFile(body, path)
                        job.ProcessedItems <- job.ProcessedItems + 1
                        jobService.UpdateJob(job) |> ignore
                    return! loop()
                }
            loop())
    
    let ProcessRequest(index : string, body : CsvIndexingRequest, context : IOwinContext) = 
        maybe { 
            body.IndexName <- index
            do! (body :> IValidator).MaybeValidator()
            if body.HasHeaderRecord = false && body.Headers = Unchecked.defaultof<_> then 
                return! Choice2Of2
                            (Errors.MISSING_FIELD_VALUE
                             |> GenerateOperationMessage
                             |> Append("Parameter", "HasHeaderRecord, Headers")
                             |> Append
                                    ("Message", 
                                     "One of the following two parameters are required: HasHeaderRecord, Headers."))
            let! (path, isDirectory) = if Directory.Exists(body.Path) then Choice1Of2(body.Path, true)
                                       else if File.Exists(body.Path) then Choice1Of2(body.Path, false)
                                       else 
                                           Choice2Of2(Errors.MISSING_FIELD_VALUE
                                                      |> GenerateOperationMessage
                                                      |> Append("Parameter", "path")
                                                      |> Append("Message", "The passed 'path' value does not exist."))
            let jobId = Guid.NewGuid()
            let job = new Job(JobId = jobId.ToString("N"))
            jobService.UpdateJob(job) |> ignore
            bulkRequestProcessor.Post(body, path, jobId, isDirectory)
            return! Choice1Of2(jobId.ToString("N"))
        }
    
    override this.Process(index, connectionName, body, context) = 
        ((ProcessRequest(index.Value, body.Value, context)), Ok, BadRequest)
