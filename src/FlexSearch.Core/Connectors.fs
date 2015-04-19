namespace FlexSearch.Core

open System
open System.IO
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
            let addDoc (cr : CsvReader) =
                let document = new Document.Dto(body.IndexName, cr.CurrentRecord.[0])
                // The first column is always id so skip it
                headers
                |> Seq.skip 1
                |> Seq.iteri (fun i header -> document.Fields.Add(header, cr.CurrentRecord.[i+1])) 
                queueService.AddDocumentQueue(document)

            let rec generateDoc (cr : CsvReader) =
                match cr.Read() with
                | false -> ok()
                | true -> 
                    addDoc cr
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
