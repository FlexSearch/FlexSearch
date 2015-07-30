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
namespace FlexSearch.DuplicateDetection

open FlexSearch.Core
open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations.Schema
open System.Diagnostics
open System.IO
open System.Text
open System.Threading.Tasks
open System.Linq
open System.Web.UI
open System.Xml.Linq
open Microsoft.VisualBasic.FileIO
open System.Runtime.Serialization

[<Sealed>]
type Session() = 
    member val Id = Guid.NewGuid().ToString() with get
    member val SessionId = Guid.NewGuid().ToString() with get
    member val IndexName = Unchecked.defaultof<string> with get, set
    member val ProfileName = Unchecked.defaultof<string> with get, set
    member val JobStartTime = Unchecked.defaultof<DateTime> with get, set
    member val JobEndTime = Unchecked.defaultof<DateTime> with get, set
    member val SelectionQuery = Unchecked.defaultof<string> with get, set
    member val DisplayFieldName = Unchecked.defaultof<string> with get, set
    member val RecordsReturned = Unchecked.defaultof<int> with get, set
    member val RecordsAvailable = Unchecked.defaultof<int> with get, set
    member val ThreadCount = Unchecked.defaultof<int> with get, set

type TargetRecord(sessionId, sourceId) = 
    member val TargetId = 0 with get, set
    member val TargetRecordId = "" with get, set
    member val TargetDisplayName = "" with get, set
    member val TrueDuplicate = false with get, set
    member val Quality = "0" with get, set
    member val TargetScore = 0.0f with get, set

type SourceRecord(sessionId) = 
    member val SessionId = sessionId with get
    member val SourceId = 0 with get, set
    member val SourceRecordId = "" with get, set
    member val SourceDisplayName = "" with get, set
    member val SourceStatus = "0" with get, set
    member val TotalDupes = 0 with get, set
    member val TargetRecords = new List<TargetRecord>() with get, set

[<AutoOpen>]
module DuplicateDetection = 
    let sessionId = "sessionid"
    let sourceId = "sourceid"
    let targetId = "targetid"
    let recordType = "type"
    let sessionRecordType = "session"
    let sourceRecordType = "source"
    let targetRecordType = "target"
    let sourceRecordId = "sourcerecordid"
    let sourceDisplayName = "sourcedisplayname"
    let totalDupesFound = "totaldupesfound"
    let sourceStatus = "sourcestatus"
    let targetRecords = "targetrecords"
    let targetRecordId = "targetrecordid"
    let targetDisplayName = "targetdisplayname"
    let targetScore = "targetscore"
    let quality = "quality"
    let sessionProperties = "sessionproperties"
    let misc = "misc"
    let formatter = new NewtonsoftJsonFormatter() :> FlexSearch.Core.IFormatter
    
    let schema = 
        let index = new Index(IndexName = "duplicates")
        index.IndexConfiguration <- new IndexConfiguration()
        index.IndexConfiguration.DirectoryType <- DirectoryType.MemoryMapped
        index.Online <- true
        index.Fields <- [| new Field(sessionId, FieldDataType.ExactText)
                           new Field(recordType, FieldDataType.ExactText)
                           // Source record
                           new Field(sourceId, FieldDataType.Int, AllowSort = true)
                           new Field(sourceRecordId, FieldDataType.ExactText)
                           new Field(sourceDisplayName, FieldDataType.Stored)
                           new Field(totalDupesFound, FieldDataType.Int)
                           new Field(sourceStatus, FieldDataType.Int, AllowSort = true)
                           new Field(targetRecords, FieldDataType.Text)
                           // Session related
                           new Field(sessionProperties, FieldDataType.Text)
                           new Field(misc, FieldDataType.Text) |]
        index
    
    let getId() = Guid.NewGuid().ToString()
    
    let writeSessionRecord (session : Session, documentService : IDocumentService) = 
        let doc = new Document(schema.IndexName, session.Id)
        doc.Fields.Add(sessionId, session.SessionId)
        doc.Fields.Add(recordType, sessionRecordType)
        let sessionPropertiesJson = formatter.SerializeToString(session)
        assert (sessionPropertiesJson <> "{}")
        doc.Fields.Add(sessionProperties, sessionPropertiesJson)
        documentService.AddOrUpdateDocument(doc) 
        |> Log.logErrorChoice            
    
    let writeDuplicates (sourceRecord : SourceRecord, documentService : IDocumentService) = 
        let sourceDoc = new Document(schema.IndexName, getId())
        sourceDoc.Fields.Add(sessionId, sourceRecord.SessionId)
        sourceDoc.Fields.Add(sourceId, sourceRecord.SourceId.ToString())
        sourceDoc.Fields.Add(sourceRecordId, sourceRecord.SourceRecordId)
        sourceDoc.Fields.Add(sourceDisplayName, sourceRecord.SourceDisplayName)
        sourceDoc.Fields.Add(sourceStatus, sourceRecord.SourceStatus)
        sourceDoc.Fields.Add(totalDupesFound, sourceRecord.TargetRecords.Count.ToString())
        sourceDoc.Fields.Add(recordType, sourceRecordType)
        sourceDoc.Fields.Add(targetRecords, formatter.SerializeToString(sourceRecord.TargetRecords))
        documentService.AddDocument(sourceDoc) |> ignore

type DuplicateDetectionRequest() = 
    inherit DtoBase()
    member val SelectionQuery = defString with get, set
    member val DisplayName = defString with get, set
    member val ThreadCount = 1 with get, set
    member val IndexName = defString with get, set
    member val ProfileName = defString with get, set
    member val MaxRecordsToScan = Int16.MaxValue with get, set
    member val DuplicatesCount = Int16.MaxValue with get, set
    member val NextId = new AtomicLong(0L)
    override this.Validate() = ok()

type DuplicateDetectionReportRequest() = 
    inherit DtoBase()
    member val SourceFileName = defString with get, set
    member val ProfileName = defString with get, set
    member val IndexName = defString with get, set
    member val QueryString = defString with get, set
    member val SelectionQuery = defString with get, set
    member val CutOff = defDouble with get, set
    override this.Validate() = 
        this.IndexName
        |> notBlank "IndexName"
        >>= fun _ -> this.ProfileName |> notBlank "ProfileName"
        >>= fun _ -> 
            let valid = this.SourceFileName
                        |> isNotBlank
                        || this.SelectionQuery |> isNotBlank
            if not valid then 
                fail 
                <| GenericError
                       ("Either one of the field 'SourceFileName or 'SelectionQuery' is required", 
                        new ResizeArray<KeyValuePair<string, string>>())
            else ok()

[<Sealed>]
[<Name("POST-/indices/:id/duplicatedetection/:id")>]
type DuplicateDetectionHandler(indexService : IIndexService, documentService : IDocumentService, searchService : ISearchService) = 
    inherit HttpHandlerBase<DuplicateDetectionRequest, Guid>()
    
    do 
        if not (indexService.IndexExists(schema.IndexName)) then 
            match indexService.AddIndex(schema) with
            | Choice1Of2(_) -> ()
            | Choice2Of2(error) -> Logger.Log(error)
    
    let duplicateRecordCheck (req : DuplicateDetectionRequest, record : Dictionary<string, string>, session : Session) = 
        let query = new SearchQuery(session.IndexName, String.Empty, SearchProfile = session.ProfileName)
        query.Columns <- [| session.DisplayFieldName |]
        query.ReturnFlatResult <- true
        query.ReturnScore <- true
        match searchService.Search(query, record) with
        | Choice1Of2(results) -> 
            if results.Meta.RecordsReturned > 1 then 
                !>"Duplicate Found"
                let header = 
                    new SourceRecord(session.SessionId, SourceRecordId = record.[Constants.IdField], 
                                     SourceDisplayName = record.[session.DisplayFieldName])
                let docs = results |> toFlatResults
                let mutable i = 1
                for result in docs.Documents do
                    let resultScore = float32 result.[Constants.Score]
                    if not (result.[Constants.IdField] = header.SourceRecordId) then 
                        let score = resultScore / docs.Meta.BestScore * 100.0f
                        let targetRecord = 
                            new TargetRecord(session.SessionId, header.SourceId, TargetId = i, 
                                             TargetDisplayName = result.[session.DisplayFieldName], TargetScore = score, 
                                             TargetRecordId = result.[Constants.IdField])
                        i <- i + 1
                        header.TargetRecords.Add(targetRecord)
                if header.TargetRecords.Count >= 1 then 
                    header.SourceId <- int (req.NextId.Increment())
                    writeDuplicates (header, documentService)
        | _ -> ()
    
    let performDuplicateDetection (jobId, indexWriter : IndexWriter.T, req : DuplicateDetectionRequest) = 
        let session = new Session()
        session.IndexName <- req.IndexName
        session.ProfileName <- req.ProfileName
        session.DisplayFieldName <- req.DisplayName
        session.JobStartTime <- DateTime.Now
        session.ThreadCount <- req.ThreadCount
        session.SelectionQuery <- req.SelectionQuery
        let parallelOptions = new ParallelOptions(MaxDegreeOfParallelism = session.ThreadCount)
        let mainQuery = 
            new SearchQuery(session.IndexName, req.SelectionQuery, Count = int req.MaxRecordsToScan, 
                                ReturnFlatResult = true, Columns = [| "*" |])
        let resultC = searchService.Search(mainQuery)
        match resultC with
        | Choice1Of2(result) -> 
            (!>) "Main Query Records Returned:%i" result.Meta.RecordsReturned
            let records = result |> toFlatResults
            session.RecordsReturned <- result.Meta.RecordsReturned
            session.RecordsAvailable <- result.Meta.TotalAvailable
            writeSessionRecord (session, documentService) |> ignore
            try 
                let _ = 
                    Parallel.ForEach(records.Documents, parallelOptions, 
                                     fun record loopState -> 
                                         if loopState.IsStopped then ()
                                         else if req.NextId.Value >= (int64) req.DuplicatesCount then 
                                             loopState.Stop()
                                         else duplicateRecordCheck (req, record, session))
                ()
            with :? AggregateException as e -> Logger.Log(e, MessageKeyword.Plugin, MessageLevel.Warning)
            session.JobEndTime <- DateTime.Now
            writeSessionRecord (session, documentService) |> ignore
        | Choice2Of2(err) -> Logger.Log(err)
        !>"Dedupe Session Finished."
    
    let requestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (jobId, indexWriter, request) = inbox.Receive()
                    performDuplicateDetection (jobId, indexWriter, request) |> ignore
                    return! loop()
                }
            loop())
    
    override __.Process(request, body) = 
        body.Value.IndexName <- request.ResId.Value
        body.Value.ProfileName <- request.SubResId.Value
        match indexService.IsIndexOnline(request.ResId.Value) with
        | Choice1Of2(writer) -> 
            match writer.Settings.SearchProfiles.TryGetValue(request.SubResId.Value) with
            | true, _ -> 
                let jobId = Guid.NewGuid()
                requestProcessor.Post(jobId, writer, body.Value)
                SuccessResponse(jobId, Ok)
            | _ -> FailureResponse(UnknownSearchProfile(request.ResId.Value, request.SubResId.Value), BadRequest)
        | Choice2Of2(error) -> FailureResponse(error, BadRequest)

// ----------------------------------------------------------------------------
// Duplicate detection report related
// ----------------------------------------------------------------------------
type RecordType = 
    | Main
    | Result
    | CutOff
    | Pass of sno : int
    | FailedMany of sno : int
    | FailedZero of sno : int

type FileWriterCommands = 
    | BeginTable of aggrResultHeader : bool * record : Dictionary<string, string>
    | AddRecord of recordType : RecordType * primaryRecord : Dictionary<string, string> * record : Dictionary<string, string>
    | EndTable

type Stats() = 
    member val TotalRecords = 0 with get, set
    member val MatchedRecords = 0 with get, set
    member val NoMatchRecords = 0 with get, set
    member val OneMatchRecord = 0 with get, set
    member val TwoMatchRecord = 0 with get, set
    member val MoreThanTwoMatchRecord = 0 with get, set

[<Sealed>]
[<Name("POST-/indices/:id/duplicatedetectionreport")>]
type DuplicateDetectionReportHandler(indexService : IIndexService, searchService : ISearchService) = 
    inherit HttpHandlerBase<DuplicateDetectionReportRequest, Guid>()
    let tableHeader = """<table class="table table-bordered table-condensed"><thead><tr>"""
    
    let escapeHtml (tag : string, data : string) = 
        let element = new XElement(XName.Get(tag), data)
        element.ToString()
    
    let addElement (command : FileWriterCommands) (builder : StringBuilder) = 
        match command with
        | BeginTable(aggrHeader, record) -> 
            if aggrHeader then 
                builder.Append
                    ("""<table class="table table-bordered table-condensed"><thead><tr><th>SNo.</th><th>Status</th>""") 
                |> ignore
            else builder.Append("""<table class="table table-bordered table-condensed"><thead><tr>""") |> ignore
            for pair in record do
                builder.Append(escapeHtml ("th", pair.Key)) |> ignore
            builder.Append("""</tr></thead><tbody>""") |> ignore
        | AddRecord(recordType, primaryRecord, record) -> 
            match recordType with
            | Main -> builder.Append("""<tr class="active">""") |> ignore
            | Result -> builder.Append("<tr>") |> ignore
            | CutOff -> builder.Append("""<tr class="active">""") |> ignore
            | FailedZero(no) -> 
                builder.Append(sprintf """<tr class="warning"><td>%i</td><td>Failed Zero</td>""" no) |> ignore
            | FailedMany(no) -> 
                builder.Append(sprintf """<tr class="warning"><td>%i</td><td>Failed Many</td>""" no) |> ignore
            | Pass(no) -> builder.Append(sprintf """<tr class="success"><td>%i</td><td>Passed</td>""" no) |> ignore
            for pair in primaryRecord do
                // This is necessary to match column names
                match record.TryGetValue(pair.Key) with
                | true, value -> builder.Append(escapeHtml ("td", value)) |> ignore
                | _ -> builder.Append(escapeHtml ("td", "")) |> ignore
            // Add score if present
            if record.ContainsKey(Constants.Score) then 
                builder.Append(escapeHtml ("td", record.[Constants.Score])) |> ignore
            builder.Append("</tr>") |> ignore
        | EndTable -> builder.Append("</tbody></table>") |> ignore
    
    let performDedupe (record : string [], headers : string [], request : DuplicateDetectionReportRequest, stats : Stats) = 
        let primaryRecord = 
            let p = dict<string>()
            headers |> Array.iteri (fun i header -> p.Add(header, record.[i]))
            p
        
        let query = new SearchQuery(request.IndexName, String.Empty, SearchProfile = request.ProfileName)
        query.Columns <- headers
        query.ReturnFlatResult <- true
        query.ReturnScore <- true
        // Override the cutoff value if provided
        if request.CutOff <> 0.0 then 
            query.OverrideProfileOptions <- true
            query.CutOff <- request.CutOff
        let builder = new StringBuilder()
        let aggrBuilder = new StringBuilder()
        // Write input row
        builder.Append(sprintf """<hr/><h4>Input Record: %i</h4>""" stats.TotalRecords) |> ignore
        builder |> addElement (BeginTable(false, primaryRecord))
        builder |> addElement (AddRecord(Main, primaryRecord, primaryRecord))
        match searchService.Search(query, primaryRecord) with
        | Choice1Of2(results) -> 
            let docs = results |> toFlatResults
            match docs.Documents.Count() with
            | 0 -> 
                stats.NoMatchRecords <- stats.NoMatchRecords + 1
                aggrBuilder |> addElement (AddRecord(FailedZero(stats.TotalRecords), primaryRecord, primaryRecord))
            | 1 -> 
                stats.OneMatchRecord <- stats.OneMatchRecord + 1
                aggrBuilder |> addElement (AddRecord(Pass(stats.TotalRecords), primaryRecord, primaryRecord))
            | 2 -> 
                stats.TwoMatchRecord <- stats.TwoMatchRecord + 1
                aggrBuilder |> addElement (AddRecord(FailedMany(stats.TotalRecords), primaryRecord, primaryRecord))
            | x when x > 2 -> 
                stats.MoreThanTwoMatchRecord <- stats.MoreThanTwoMatchRecord + 1
                aggrBuilder |> addElement (AddRecord(FailedMany(stats.TotalRecords), primaryRecord, primaryRecord))
            | _ -> ()
            for doc in docs.Documents do
                builder |> addElement (AddRecord(Result, primaryRecord, doc))
            builder |> addElement (EndTable)
        | Choice2Of2(error) -> 
            builder.Append(sprintf """%A""" error) |> ignore
            builder |> addElement (EndTable)
        (builder.ToString(), aggrBuilder.ToString())
    
    let getFileDataSource (request : DuplicateDetectionReportRequest) = 
        let reader = new TextFieldParser(request.SourceFileName)
        reader.TextFieldType <- FieldType.Delimited
        reader.SetDelimiters([| "," |])
        reader.TrimWhiteSpace <- true
        let headers = reader.ReadFields()

        let data = 
            seq { 
                while not reader.EndOfData do
                    yield reader.ReadFields()
            }
        
        (headers, data)
    
    let getIndexDataSource (request : DuplicateDetectionReportRequest) = 
        let mainQuery = 
            new SearchQuery(request.IndexName, request.SelectionQuery, Count = int Int16.MaxValue, 
                                ReturnFlatResult = true, Columns = [| "*" |])
        let result = searchService.Search(mainQuery)
        match result with
        | Choice1Of2(result) -> 
            let headers = 
                searchService.Search
                    (new SearchQuery(request.IndexName, request.SelectionQuery, Count = 1, ReturnFlatResult = true, 
                                         Columns = [| "*" |]))
                |> extract
                |> toFlatResults
                |> fun x -> 
                    let d = x.Documents.ToList().First()
                    d.Keys.ToArray()
            (!>) "Main Query Records Returned:%i" result.Meta.RecordsReturned
            let records = result |> toFlatResults
            
            let d = 
                seq { 
                    for record in records.Documents do
                        yield record.Values.ToArray()
                }
            (headers, d)
        | Choice2Of2(err) -> 
            Logger.Log(err)
            (Array.empty, Seq.empty)
    
    let getDataSource (request : DuplicateDetectionReportRequest) = 
        if isNotBlank request.SourceFileName then getFileDataSource (request)
        else getIndexDataSource (request)
    
    let generateReport (jobId, request : DuplicateDetectionReportRequest) = 
        let (headers, data) = getDataSource (request)
        let stats = new Stats()
        let builder = new StringBuilder()
        let aggrBuilder = new StringBuilder()
        
        let aggrHeader = 
            let p = dict<string>()
            headers |> Array.iteri (fun i header -> p.Add(header, ""))
            p
        aggrBuilder |> addElement (BeginTable(true, aggrHeader))
        (!>) "Starting Duplicate detection report generation using file %s" request.SourceFileName
        let mutable count = 0
        for record in data do
            stats.TotalRecords <- stats.TotalRecords + 1
            let (reportRes, aggrResult) = performDedupe (record, headers, request, stats)
            builder.AppendLine(reportRes) |> ignore
            aggrBuilder.AppendLine(aggrResult) |> ignore
            count <- count + 1
            if count % 1000 = 0 then (!>) "Dedupe report: Completed %i" count
        (!>) "Dedupe report: Completed %i" count
        !>"Generating report"
        aggrBuilder |> addElement (EndTable)
        let mutable template = File.ReadAllText(WebFolder +/ "Reports//DuplicateDetectionTemplate.html")
        template <- template.Replace("{{IndexName}}", request.IndexName)
        template <- template.Replace("{{ProfileName}}", request.ProfileName)
        template <- template.Replace("{{QueryString}}", escapeHtml ("code", request.QueryString))
        template <- template.Replace("{{CutOff}}", request.CutOff.ToString())
        template <- template.Replace("{{TotalRecords}}", stats.TotalRecords.ToString())
        template <- template.Replace
                        ("{{MatchedRecords}}", 
                         (stats.MatchedRecords + stats.OneMatchRecord + stats.TwoMatchRecord + stats.MoreThanTwoMatchRecord)
                             .ToString())
        template <- template.Replace("{{NoMatchRecords}}", stats.NoMatchRecords.ToString())
        template <- template.Replace("{{OneMatchRecord}}", stats.OneMatchRecord.ToString())
        template <- template.Replace("{{TwoMatchRecord}}", stats.TwoMatchRecord.ToString())
        template <- template.Replace("{{MoreThanTwoMatchRecord}}", stats.MoreThanTwoMatchRecord.ToString())
        template <- template.Replace("{{Results}}", builder.ToString())
        template <- template.Replace("{{AggregratedResults}}", aggrBuilder.ToString())
        File.WriteAllText
            (Constants.WebFolder +/ "Reports" 
             +/ (sprintf "%s_%s_%s_cutoff_%i_%i.html" request.IndexName request.ProfileName 
                     (Path.GetFileNameWithoutExtension(request.SourceFileName)) (int request.CutOff) 
                     (GetCurrentTimeAsLong())), template)
    
    let requestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (jobId, request) = inbox.Receive()
                    generateReport (jobId, request) |> ignore
                    return! loop()
                }
            loop())
    
    let processRequest indexName (body : DuplicateDetectionReportRequest) = 
        maybe { 
            do! body.Validate()
            let! writer = indexService.IsIndexOnline(indexName)
            match writer.Settings.SearchProfiles.TryGetValue(body.ProfileName) with
            | true, (_, profile) -> 
                body.QueryString <- profile.QueryString
                let jobId = Guid.NewGuid()
                requestProcessor.Post(jobId, body)
                return jobId
            | _ -> return! fail <| UnknownSearchProfile(indexName, body.ProfileName)
        }
    
    override __.Process(request, body) = 
        body.Value.IndexName <- request.ResId.Value
        SomeResponse(processRequest request.ResId.Value body.Value, Ok, BadRequest)
