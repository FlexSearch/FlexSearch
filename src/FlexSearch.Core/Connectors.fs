namespace FlexSearch.Core

open System
open System.IO
open System.Data
open System.Data.SqlClient
open CsvHelper
open CsvHelper.Configuration
open Microsoft.Owin




/// <summary>
/// Represents a request which can be sent to CSV connector to index CSV data.
/// </summary>
[<Sealed>]
type CsvIndexingRequest() = 
    inherit DtoBase()
    
    /// <summary>
    /// Name of the index
    /// </summary>
    member val IndexName = defString with get, set
    
    /// <summary>
    /// Signifies if the passed CSV file(s) has a header record 
    /// </summary>
    member val HasHeaderRecord = false with get, set
    
    /// <summary>
    /// The headers to be used by each column. This should only be passed when there is
    /// no header in the csv file. The first column is always assumed to be id field. Make sure
    /// in your array you always offset the column names by 1 position.
    /// </summary>
    member val Headers = defArray<string> with get, set
    
    /// <summary>
    /// The path of the folder or file to be indexed. The service will pickup all files with 
    /// .csv extension.
    /// </summary>
    member val Path = defString with get, set

    override __.Validate() =
        if __.HasHeaderRecord = false && (__.Headers |> Seq.isEmpty) 
        then fail <| Error.MissingFieldValue("HasHeaderRecord, Headers")
        else ok ()
        

/// <summary>
/// Connector for importing CSV file data into the system.
/// </summary>
[<Sealed>]
[<Name("POST-/indices/:id/csv")>]
type CsvHandler(queueService : IQueueService, jobService : IJobService) = 
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
                else defArray
            else
                body.Headers

        match headers with
        | [||] -> fail <| Error.HeaderRowIsEmpty
        | _ ->
            let rec generateDoc (cr : CsvReader) =
                match cr.Read() with
                | false -> ok()
                | true -> 
                    let document = new Document.Dto(body.IndexName, cr.CurrentRecord.[0])
                    // The first column is always id so skip it
                    headers
                    |> Seq.skip 1
                    |> Seq.iteri (fun i header -> document.Fields.Add(header, cr.CurrentRecord.[i+1])) 
                    queueService.AddDocumentQueue(document)

                    generateDoc cr
                
            generateDoc reader
    
    let bulkRequestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (body : CsvIndexingRequest, path : string, jobId : Guid, isFolder : bool) = inbox.Receive()
                    let job = new Job(JobId = jobId.ToString("N"), Status = JobStatus.InProgress)
                    jobService.UpdateJob(job) |> ignore

                    let execFileJob filePath = 
                        match ProcessFile(body, filePath) with
                        | Choice1Of2() -> 
                            job.ProcessedItems <- job.ProcessedItems + 1
                            jobService.UpdateJob(job) |> ignore
                        | Choice2Of2(error) -> 
                            job.FailedItems <- job.FailedItems + 1
                            jobService.UpdateJob(job) |> ignore

                    if isFolder then 
                        Directory.EnumerateFiles(path) |> Seq.iter execFileJob
                    else 
                        execFileJob path

                    // Mark the Job as Completed with/without errors
                    if job.FailedItems > 0
                    then job.Status <- JobStatus.CompletedWithErrors
                    else job.Status <- JobStatus.Completed
                    jobService.UpdateJob(job) |> ignore

                    return! loop()
                }
            loop())
    
    let processRequest index (body : CsvIndexingRequest) = 
        let pathValidation() = 
            if Directory.Exists(body.Path) then ok(body.Path, true)
            else if File.Exists(body.Path) then ok(body.Path, false)
            else fail <| Error.PathDoesNotExist body.Path
        let postBulkRequestMessage (path, isDirectory) =
            let jobId = Guid.NewGuid()
            let job = new Job(JobId = jobId.ToString("N"))
            jobService.UpdateJob(job) |> ignore
            bulkRequestProcessor.Post(body, path, jobId, isDirectory)
            ok <| jobId.ToString("N")

        body.IndexName <- index

        body.Validate()
        >>= pathValidation
        >>= postBulkRequestMessage

    override this.Process(request, body) = 
        SomeResponse(processRequest request.ResName body.Value, Ok, BadRequest)



/// <summary>
/// Represents a request which can be sent to Sql connector to index SQL data
/// </summary>
[<Sealed>]
type SqlIndexingRequest() = 
    inherit DtoBase()
    
    /// <summary>
    /// Name of the index
    /// </summary>
    member val IndexName = defString with get, set
    
    /// <summary>
    /// The query to be used to fetch data from Sql server
    /// </summary>
    member val Query = defString with get, set
    
    /// <summary>
    /// Connection string used to connect to the server
    /// </summary>
    member val ConnectionString = defString with get, set
    
    /// <summary>
    /// Signifies if all updates to the index are create
    /// </summary>
    member val ForceCreate = true with get, set
    
    /// <summary>
    /// Signifies if the connector should create a job for the task and return a jobId which can be used
    /// to check the status of the job.
    /// </summary>
    member val CreateJob = false with get, set

    override __.Validate() = ok ()

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
                    let document = new Document.Dto(IndexName = request.IndexName, Id = reader.[0].ToString())
                    for i = 1 to reader.FieldCount - 1 do
                        document.Fields.Add(reader.GetName(i), reader.GetValue(i).ToString())
                    if request.ForceCreate then queueService.AddDocumentQueue(document)
                    else queueService.AddOrUpdateDocumentQueue(document)
                    rows <- rows + 1
                    if jobId |> isNotBlank && rows % 5000 = 0 then 
                        let job = 
                            new Job(JobId = jobId, Status = JobStatus.InProgress, Message = "", ProcessedItems = rows)
                        jobService.UpdateJob(job) |> ignore

                if jobId |> isNotBlank then 
                    let job = 
                        new Job(JobId = jobId, Status = JobStatus.Completed, Message = "Completed", 
                                ProcessedItems = rows)
                    jobService.UpdateJob(job) |> ignore
                    Log.info <| sprintf "SQL connector: %A" job
            else 
                if jobId |> isNotBlank then 
                    let job = 
                        new Job(JobId = jobId, Status = JobStatus.CompletedWithErrors, Message = "No rows returned.", 
                                ProcessedItems = rows)
                    jobService.UpdateJob(job) |> ignore
                Log.error <| sprintf "SQL connector error. No rows returned. Query:{%s}" request.Query
        with e -> 
            if jobId |> isNotBlank then 
                let job = new Job(JobId = jobId, Status = JobStatus.CompletedWithErrors, Message = e.Message)
                jobService.UpdateJob(job) |> ignore
            Log.error <| sprintf "SQL connector error: %s" (e |> exceptionPrinter)
    
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
        let createJob () = 
            if body.CreateJob then 
                let guid = Guid.NewGuid()
                bulkRequestProcessor.Post(body, guid.ToString())
                guid.ToString()
            else 
                ExecuteSql(body, "")
                ""
            |> Choice1Of2
        body.IndexName <- index

        body.Validate()
        >>= createJob
    
    override this.Process(request, body) = 
        SomeResponse(processRequest request.ResName body.Value, Ok, BadRequest)