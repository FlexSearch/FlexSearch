// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open System
open System.Data
open System.Data.SqlClient
open System.IO
open Microsoft.VisualBasic.FileIO

module CsvHelpers = 
    let readCsv (filePath : string, header : string [] option) = 
        use reader = new TextFieldParser(filePath)
        reader.TextFieldType <- FieldType.Delimited
        reader.SetDelimiters([| "," |])
        let headers = 
            match header with
            | Some(h) -> h
            | None -> reader.ReadFields()
        
        // It will return on the first error encountered. We need a parser which can
        // automatically surpress errors. 
        let records = 
            try 
                seq { 
                    while not reader.EndOfData do
                        let record = reader.ReadFields()
                        yield record
                }
            with e -> 
                Logger.Log(e, MessageKeyword.Plugin, MessageLevel.Warning)
                Seq.empty
        
        (headers, records)

/// Represents a request which can be sent to CSV connector to index CSV data.
[<Sealed>]
type CsvIndexingRequest() = 
    inherit DtoBase()
    
    /// Name of the index
    member val IndexName = defString with get, set
    
    /// Signifies if the passed CSV file(s) has a header record 
    member val HasHeaderRecord = false with get, set
    
    /// The headers to be used by each column. This should only be passed when there is
    /// no header in the csv file. The first column is always assumed to be id field. Make sure
    /// in your array you always offset the column names by 1 position.
    member val Headers = defArray<string> with get, set
    
    /// The path of the folder or file to be indexed. The service will pickup all files with 
    /// .csv extension.
    member val Path = defString with get, set
    
    override __.Validate() = 
        if __.HasHeaderRecord = false && (__.Headers |> Seq.isEmpty) then 
            fail <| MissingFieldValue("HasHeaderRecord, Headers")
        else okUnit

/// Connector for importing CSV file data into the system.
[<Sealed>]
[<Name("POST-/indices/:id/csv")>]
type CsvHandler(queueService : IQueueService, indexService : IIndexService, jobService : IJobService) = 
    inherit HttpHandlerBase<CsvIndexingRequest, string>()
    
    //    let ProcessFileUsingCsvHelper(body : CsvIndexingRequest, path : string) = 
    //        let configuration = new CsvConfiguration()
    //        configuration.HasHeaderRecord <- body.HasHeaderRecord
    //        use textReader = File.OpenText(path)
    //        let parser = new CsvParser(textReader, configuration)
    //        let reader = new CsvReader(parser)
    //        
    //        let headers = 
    //            if body.HasHeaderRecord then 
    //                if reader.Read() then reader.FieldHeaders
    //                else defArray
    //            else body.Headers
    //        match headers with
    //        | [||] -> fail <| Error.HeaderRowIsEmpty
    //        | _ -> 
    //                while reader.Read() do
    //                    let document = new Document.Dto(body.IndexName, reader.CurrentRecord.[0])
    //                    // The first column is always id so skip it
    //                    headers
    //                    |> Seq.skip 1
    //                    |> Seq.iteri (fun i header -> document.Fields.Add(header, reader.CurrentRecord.[i + 1]))
    //                    queueService.AddDocumentQueue(document)
    //                okUnit
    let processFile (body : CsvIndexingRequest, path : string) = 
        use reader = new TextFieldParser(path)
        (!>) "Parsing CSV file at: %s" path
        reader.TextFieldType <- FieldType.Delimited
        reader.SetDelimiters([| "," |])
        let headers = 
            if body.HasHeaderRecord then reader.ReadFields()
            else body.Headers
        match headers with
        | [||] -> 
            (!>) "CSV Parsing failed. No header row. File: %s" path
            fail <| HeaderRowIsEmpty
        | _ -> 
            let mutable rows = 0L
            while not reader.EndOfData do
                try 
                    rows <- rows + 1L
                    let currentRow = reader.ReadFields()
                    let document = new Document(body.IndexName, currentRow.[0])
                    document.TimeStamp <- 0L
                    // The first column is always id so skip it
                    for i = 1 to currentRow.Length - 1 do
                        document.Fields.Add(headers.[i], currentRow.[i])
                    queueService.AddDocumentQueue(document)
                with e -> 
                    (!>) "CSV Parsing error: %A" e
                    Logger.Log(e, MessageKeyword.Plugin, MessageLevel.Warning)
            (!>) "CSV Parsing finished. Processed Rows:%i File: %s" rows path
            okUnit
    
    let bulkRequestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (body : CsvIndexingRequest, path : string, jobId : Guid, isFolder : bool) = inbox.Receive()
                    let job = new Job(JobId = jobId.ToString("N"), Status = JobStatus.InProgress)
                    jobService.UpdateJob(job) |> ignore
                    let execFileJob filePath = 
                        match processFile (body, filePath) with
                        | Ok() -> 
                            job.ProcessedItems <- job.ProcessedItems + 1
                            jobService.UpdateJob(job) |> ignore
                        | Fail(error) -> 
                            job.FailedItems <- job.FailedItems + 1
                            jobService.UpdateJob(job) |> ignore
                    if isFolder then Directory.EnumerateFiles(path) |> Seq.iter execFileJob
                    else execFileJob path
                    // Mark the Job as Completed with/without errors
                    if job.FailedItems > 0 then job.Status <- JobStatus.CompletedWithErrors
                    else job.Status <- JobStatus.Completed
                    jobService.UpdateJob(job) |> ignore
                    return! loop()
                }
            loop())
    
    let processRequest index (body : CsvIndexingRequest) = 
        let pathValidation() = 
            if Directory.Exists(body.Path) then ok (body.Path, true)
            else if File.Exists(body.Path) then ok (body.Path, false)
            else fail <| PathDoesNotExist body.Path
        
        let postBulkRequestMessage (path, isDirectory) = 
            let jobId = Guid.NewGuid()
            let job = new Job(JobId = jobId.ToString("N"))
            jobService.UpdateJob(job) |> ignore
            bulkRequestProcessor.Post(body, path, jobId, isDirectory)
            ok <| jobId.ToString("N")
        
        body.IndexName <- index
        match indexService.IsIndexOnline(index) with
        | Ok(_) -> body.Validate() >>= pathValidation >>= postBulkRequestMessage
        | Fail(error) -> fail <| error
    
    override __.Process(request, body) = SomeResponse(processRequest request.ResId.Value body.Value, Ok, BadRequest)

/// Represents a request which can be sent to Sql connector to index SQL data
[<Sealed>]
type SqlIndexingRequest() = 
    inherit DtoBase()
    
    /// Name of the index
    member val IndexName = defString with get, set
    
    /// The query to be used to fetch data from Sql server
    member val Query = defString with get, set
    
    /// Connection string used to connect to the server
    member val ConnectionString = defString with get, set
    
    /// Signifies if all updates to the index are create
    member val ForceCreate = true with get, set
    
    /// Signifies if the connector should create a job for the task and return a jobId which can be used
    /// to check the status of the job.
    member val CreateJob = false with get, set
    
    override __.Validate() = okUnit

[<Sealed>]
[<Name("POST-/indices/:id/sql")>]
type SqlHandler(queueService : IQueueService, jobService : IJobService) = 
    inherit HttpHandlerBase<SqlIndexingRequest, string>()
    
    let ExecuteSql(request : SqlIndexingRequest, jobId) = 
        let isNotBlank = isBlank >> not
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
            if reader.HasRows then 
                while reader.Read() do
                    let document = new Document(IndexName = request.IndexName, Id = reader.[0].ToString())
                    for i = 1 to reader.FieldCount - 1 do
                        document.Fields.Add(reader.GetName(i), reader.GetValue(i).ToString())
                    if request.ForceCreate then queueService.AddDocumentQueue(document)
                    else queueService.AddOrUpdateDocumentQueue(document)
                    rows <- rows + 1
                    if rows % 5000 = 0 then jobService.UpdateJob(jobId, JobStatus.InProgress, rows)
                jobService.UpdateJob(jobId, JobStatus.Completed, rows)
                Logger.Log
                    (sprintf "SQL connector: Job Finished. Query:{%s}. Index:{%s}" request.Query request.IndexName, 
                     MessageKeyword.Plugin, MessageLevel.Verbose)
            else 
                jobService.UpdateJob(jobId, JobStatus.CompletedWithErrors, rows, "No rows returned.")
                Logger.Log
                    (sprintf "SQL connector error. No rows returned. Query:{%s}" request.Query, MessageKeyword.Plugin, 
                     MessageLevel.Error)
        with e -> 
            jobService.UpdateJob(jobId, JobStatus.CompletedWithErrors, 0, (e |> exceptionPrinter))
            Logger.Log
                (sprintf "SQL connector error: %s" (e |> exceptionPrinter), MessageKeyword.Plugin, MessageLevel.Error)
    
    let bulkRequestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (body, jobId) = inbox.Receive()
                    ExecuteSql(body, jobId)
                    return! loop()
                }
            loop())
    
    let processRequest index (body : SqlIndexingRequest) = 
        let createJob() = 
            if body.CreateJob then 
                let guid = Guid.NewGuid()
                bulkRequestProcessor.Post(body, guid.ToString())
                guid.ToString()
            else 
                ExecuteSql(body, "")
                ""
            |> ok
        body.IndexName <- index
        body.Validate() >>= createJob
    
    override __.Process(request, body) = SomeResponse(processRequest request.ResId.Value body.Value, Ok, BadRequest)
