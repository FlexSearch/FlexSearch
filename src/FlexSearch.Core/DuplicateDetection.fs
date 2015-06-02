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
open System.Data.Entity
open System.Diagnostics
open System.IO
open System.Text
open System.Threading.Tasks
open System.Linq
open System.Web.UI
open System.Xml.Linq
open Microsoft.VisualBasic.FileIO

type LineItem() = 
    let mutable header = Unchecked.defaultof<Header>
    member val LineItemId = Unchecked.defaultof<int> with get, set
    member val SecondaryRecordId = Unchecked.defaultof<string> with get, set
    member val DisplayName = Unchecked.defaultof<string> with get, set
    member val Score = Unchecked.defaultof<float> with get, set
    member val Status = Unchecked.defaultof<int> with get, set
    member val HeaderId = Unchecked.defaultof<int> with get, set
    abstract Header : Header with get, set
    
    override __.Header 
        with get () = header
        and set (v) = header <- v

and Header() = 
    let mutable lineItems = Unchecked.defaultof<ICollection<LineItem>>
    let mutable session = Unchecked.defaultof<Session>
    member val HeaderId = Unchecked.defaultof<int> with get, set
    member val PrimaryRecordId = Unchecked.defaultof<string> with get, set
    member val DisplayName = Unchecked.defaultof<string> with get, set
    member val Score = Unchecked.defaultof<float> with get, set
    member val Status = Unchecked.defaultof<int> with get, set
    abstract LineItems : ICollection<LineItem> with get, set
    
    override __.LineItems 
        with get () = lineItems
        and set (v) = lineItems <- v
    
    member val SessionId = Unchecked.defaultof<int> with get, set
    abstract Session : Session with get, set
    
    override __.Session 
        with get () = session
        and set (v) = session <- v

and Session() = 
    let mutable headers = Unchecked.defaultof<ICollection<Header>>
    member val SessionId = Unchecked.defaultof<int> with get, set
    member val IndexName = Unchecked.defaultof<string> with get, set
    member val ProfileName = Unchecked.defaultof<string> with get, set
    
    [<Column(TypeName = "DateTime2")>]
    member val JobStartTime = Unchecked.defaultof<DateTime> with get, set
    
    [<Column(TypeName = "DateTime2")>]
    member val JobEndTime = Unchecked.defaultof<DateTime> with get, set
    
    [<Column(TypeName = "DateTime2")>]
    member val RangeStartTime = Unchecked.defaultof<DateTime> with get, set
    
    [<Column(TypeName = "DateTime2")>]
    member val RangeEndTime = Unchecked.defaultof<DateTime> with get, set
    
    member val DisplayFieldName = Unchecked.defaultof<string> with get, set
    member val DateTimeField = Unchecked.defaultof<string> with get, set
    member val RecordsReturned = Unchecked.defaultof<int> with get, set
    member val RecordsAvailable = Unchecked.defaultof<int> with get, set
    member val ThreadCount = Unchecked.defaultof<int> with get, set
    abstract Headers : ICollection<Header> with get, set
    
    override __.Headers 
        with get () = headers
        and set (v) = headers <- v

type DataContext(connectionString : string) = 
    inherit DbContext(connectionString)
    [<DefaultValue>]
    val mutable headers : DbSet<Header>
    
    member x.Headers 
        with get () = x.headers
        and set v = x.headers <- v
    
    [<DefaultValue>]
    val mutable lineItems : DbSet<LineItem>
    
    member x.LineItems 
        with get () = x.lineItems
        and set v = x.lineItems <- v
    
    [<DefaultValue>]
    val mutable sessions : DbSet<Session>
    
    member x.Sessions 
        with get () = x.sessions
        and set v = x.sessions <- v

type DuplicateDetectionRequest() = 
    inherit DtoBase()
    member val StartDate = 0L with get, set
    member val EndDate = 0L with get, set
    member val DateTimeField = defString with get, set
    member val DisplayName = defString with get, set
    member val ThreadCount = 1 with get, set
    member val GenerateReport = false with get, set
    member val ReportName = defString with get, set
    member val ConnectionString = defString with get, set
    member val IndexName = defString with get, set
    member val ProfileName = defString with get, set
    override this.Validate() = ok()

type RecordType = 
    | Main
    | Result
    | CutOff
    | Pass of sno : int
    | FailedMany of sno : int
    | FailedZero of sno : int

type FileWriterCommands = 
    | BeginTable of aggrResultHeader : bool * record : Dictionary<string, string>
    | AddRecord of recordType : RecordType * primaryRecord: Dictionary<string, string> * record : Dictionary<string, string>
    | EndTable

[<Sealed>]
[<Name("POST-/indices/:id/duplicatedetection/:id")>]
type DuplicateDetectionHandler(indexService : IIndexService, searchService : ISearchService) = 
    inherit HttpHandlerBase<DuplicateDetectionRequest, Guid>()
    
    let writeSessionBeginInfo (req : DuplicateDetectionRequest, session) = 
        use context = new DataContext(req.ConnectionString)
        let session = context.Sessions.Add(session)
        context.SaveChanges() |> ignore
        session
    
    //    /// Compile the complete report
    //    let generateReport (session : Session, req : DuplicateDetectionRequest) = 
    //        let mutable template = File.ReadAllText(WebFolder +/ "Reports//DuplicateDetectionTemplate.html")
    //        let body = File.ReadAllText(req.ReportName)
    //        template <- template.Replace("{{IndexName}}", req.IndexName)
    //        template <- template.Replace("{{ProfileName}}", req.ProfileName)
    //        template <- template.Replace("{{StartRange}}", req.StartDate.ToString())
    //        template <- template.Replace("{{EndRange}}", req.EndDate.ToString())
    //        template <- template.Replace("{{TotalChecked}}", session.RecordsReturned.ToString())
    //        template <- template.Replace("{{TotalDuplicates}}", "")
    //        template <- template.Replace("{{Results}}", body)
    //        File.WriteAllText(sprintf "%s.html" req.ReportName, template)
    //        File.Delete(req.ReportName)
    //    
    let writeSessionEndInfo (req : DuplicateDetectionRequest, session : Session) = 
        use context = new DataContext(req.ConnectionString)
        context.SaveChanges() |> ignore
    
    //        if req.GenerateReport then generateReport (session, req)
    //    let fileWriter = 
    //        MailboxProcessor.Start(fun inbox -> 
    //            let rec loop() = 
    //                async { 
    //                    let builder = new StringBuilder()
    //                    let! (command : FileWriterCommands, filePath) = inbox.Receive()
    //                    builder.Clear() |> ignore
    //                    match command with
    //                    | BeginTable(record) -> 
    //                        builder.Append("""<table class="table table-bordered table-condensed"><thead><tr>""") |> ignore
    //                        for pair in record do
    //                            builder.Append(sprintf "<th>%s</th>" pair.Key) |> ignore
    //                        builder.Append("""</tr></thead><tbody>""") |> ignore
    //                    | AddRecord(recordType, record) -> 
    //                        match recordType with
    //                        | N
    //                        if mainRecord then builder.Append("""<tr class="active">""") |> ignore
    //                        else builder.Append("<tr>") |> ignore
    //                        for pair in record do
    //                            builder.Append(sprintf "<td>%s</td>" pair.Value) |> ignore
    //                        builder.Append("</tr>") |> ignore
    //                    | EndTable -> builder.Append("</tbody></table><hr/>") |> ignore
    //                    File.AppendAllText(filePath, builder.ToString())
    //                    return! loop()
    //                }
    //            loop())
    let duplicateRecordCheck (req : DuplicateDetectionRequest, record : Dictionary<string, string>, session : Session) = 
        let query = new SearchQuery.Dto(session.IndexName, String.Empty, SearchProfile = session.ProfileName)
        if req.GenerateReport then query.Columns <- [| "*" |]
        else query.Columns <- [| session.DisplayFieldName |]
        query.ReturnFlatResult <- true
        query.ReturnScore <- true
        match searchService.Search(query, record) with
        | Choice1Of2(results) -> 
            if results.Meta.RecordsReturned > 1 then 
                Debug.WriteLine("Duplicate Found")
                use context = new DataContext(req.ConnectionString)
                let header = 
                    context.headers.Add
                        (new Header(PrimaryRecordId = record.[Constants.IdField], SessionId = session.SessionId, 
                                    DisplayName = record.[session.DisplayFieldName]))
                let docs = results |> toFlatResults
                for result in docs.Documents do
                    // The returned record is same as the passed record. Save the score for relative
                    // scoring later
                    let score = float result.[Constants.Score]
                    if result.[Constants.IdField] = header.PrimaryRecordId then header.Score <- score
                    else 
                        let score = 
                            if header.Score >= score then score / header.Score * 100.0
                            else 0.0
                        context.LineItems.Add
                            (new LineItem(SecondaryRecordId = result.[Constants.IdField], 
                                          DisplayName = result.[session.DisplayFieldName], Score = score, 
                                          HeaderId = header.HeaderId)) |> ignore
                context.SaveChanges() |> ignore
        | _ -> ()
    
    let performDuplicateDetection (jobId, indexWriter : IndexWriter.T, req : DuplicateDetectionRequest) = 
        try 
            let session = new Session()
            session.IndexName <- req.IndexName
            session.ProfileName <- req.ProfileName
            session.RangeStartTime <- parseDate <| req.StartDate.ToString()
            session.RangeEndTime <- parseDate <| req.EndDate.ToString()
            session.DisplayFieldName <- req.DisplayName
            session.DateTimeField <- req.DateTimeField
            session.JobStartTime <- DateTime.Now
            session.ThreadCount <- req.ThreadCount
            let parallelOptions = new ParallelOptions(MaxDegreeOfParallelism = session.ThreadCount)
            let dateTimeField = session.DateTimeField //indexWriter.GetSchemaName(session.DateTimeField)
            let mainQueryString = 
                sprintf "%s > '%i' AND %s < '%i'" dateTimeField (session.RangeStartTime |> dateToFlexFormat) 
                    dateTimeField (session.RangeEndTime |> dateToFlexFormat)
            Debug.WriteLine("Main Query:" + mainQueryString)
            let mainQuery = 
                new SearchQuery.Dto(session.IndexName, mainQueryString, Count = (int32) System.Int16.MaxValue)
            // TODO: Future optimization: bring only the required columns
            mainQuery.ReturnFlatResult <- true
            mainQuery.Columns <- [| "*" |]
            let resultC = searchService.Search(mainQuery)
            match resultC with
            | Choice1Of2(result) -> 
                Debug.WriteLine("Main Query Records Returned:" + result.Meta.RecordsReturned.ToString())
                let records = result |> toFlatResults
                session.RecordsReturned <- result.Meta.RecordsReturned
                session.RecordsAvailable <- result.Meta.TotalAvailable
                let session = writeSessionBeginInfo (req, session)
                try 
                    let _ = 
                        Parallel.ForEach
                            (records.Documents, parallelOptions, 
                             fun record -> duplicateRecordCheck (req, record, session))
                    ()
                with :? AggregateException as e -> Logger.Log (e, MessageKeyword.Plugin, MessageLevel.Warning)
                session.JobEndTime <- DateTime.Now
                writeSessionEndInfo (req, session)
            | Choice2Of2(err) -> Logger.Log (err)
        with e -> Logger.Log (e, MessageKeyword.Plugin, MessageLevel.Warning)
        Debug.WriteLine("Dedupe Session Finished.")
    
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
            body.Value.StartDate <- request.OwinContext 
                                    |> longFromQueryString "startdate" (DateTime.Now.AddDays(-1.0) |> dateToFlexFormat)
            body.Value.EndDate <- request.OwinContext 
                                  |> longFromQueryString "enddate" (DateTime.Now |> dateToFlexFormat)
            body.Value.DateTimeField <- request.OwinContext 
                                        |> stringFromQueryString "datetimefield" Constants.LastModifiedField
            body.Value.DisplayName <- request.OwinContext |> stringFromQueryString "displayname" Constants.IdField
            body.Value.ThreadCount <- request.OwinContext |> intFromQueryString "threadcount" 1
            if body.Value.GenerateReport && isBlank (body.Value.ReportName) then 
                let dir = createDir (Constants.WebFolder +/ "Reports")
                body.Value.ReportName <- dir 
                                         +/ (sprintf "%s-%s-%i" body.Value.IndexName body.Value.ProfileName 
                                                 body.Value.StartDate)
            match writer.Settings.SearchProfiles.TryGetValue(request.SubResId.Value) with
            | true, _ -> 
                let jobId = Guid.NewGuid()
                // Try creating sql context to see if the target is available
                try 
                    let context = new DataContext(body.Value.ConnectionString)
                    context.Database.Exists() |> ignore
                    requestProcessor.Post(jobId, writer, body.Value)
                    SuccessResponse(jobId, Ok)
                with e -> 
                    let om = 
                        { Message = "Unable to connect to the sql server using the provided connection string."
                          ErrorCode = "SQLConnectionError"
                          Properties = [| ("exception", exceptionPrinter e) |] }
                    FailureOpMsgResponse(om, BadRequest)
            | _ -> FailureResponse(UnknownSearchProfile(request.ResId.Value, request.SubResId.Value), BadRequest)
        | Choice2Of2(error) -> FailureResponse(error, BadRequest)

type DuplicateDetectionReportRequest() = 
    inherit DtoBase()
    member val SourceFileName = defString with get, set
    member val ProfileName = defString with get, set
    member val IndexName = defString with get, set
    member val QueryString = defString with get, set
    member val CutOff = defDouble with get, set
    override this.Validate() = this.IndexName
                               |> notBlank "IndexName"
                               >>= fun _ -> this.ProfileName |> notBlank "ProfileName"
                               >>= fun _ -> this.SourceFileName |> notBlank "SourceFileName"

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
                | true, value -> 
                    builder.Append(escapeHtml ("td", value)) |> ignore
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
        
        let query = new SearchQuery.Dto(request.IndexName, String.Empty, SearchProfile = request.ProfileName)
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
                aggrBuilder |> addElement (AddRecord(FailedZero(stats.TotalRecords),primaryRecord, primaryRecord))
            | 1 -> 
                stats.OneMatchRecord <- stats.OneMatchRecord + 1
                aggrBuilder |> addElement (AddRecord(Pass(stats.TotalRecords),primaryRecord, primaryRecord))
            | 2 -> 
                stats.TwoMatchRecord <- stats.TwoMatchRecord + 1
                aggrBuilder |> addElement (AddRecord(FailedMany(stats.TotalRecords),primaryRecord, primaryRecord))
            | x when x > 2 -> 
                stats.MoreThanTwoMatchRecord <- stats.MoreThanTwoMatchRecord + 1
                aggrBuilder |> addElement (AddRecord(FailedMany(stats.TotalRecords),primaryRecord, primaryRecord))
            | _ -> ()
            for doc in docs.Documents do
                builder |> addElement (AddRecord(Result, primaryRecord, doc))
            builder |> addElement (EndTable)
        | Choice2Of2(error) -> 
            builder.Append(sprintf """%A""" error) |> ignore
            builder |> addElement (EndTable)
        (builder.ToString(), aggrBuilder.ToString())
    
    let generateReport (jobId, request : DuplicateDetectionReportRequest) = 
        //let (headers, records) = CsvHelpers.readCsv (request.SourceFileName, None)
        use reader = new TextFieldParser(request.SourceFileName)
        reader.TextFieldType <- FieldType.Delimited
        reader.SetDelimiters([| "," |])
        reader.TrimWhiteSpace <- true
        let headers = reader.ReadFields()
        let stats = new Stats()
        let builder = new StringBuilder()
        let aggrBuilder = new StringBuilder()
        
        let aggrHeader = 
            let p = dict<string>()
            headers |> Array.iteri (fun i header -> p.Add(header, ""))
            p
        aggrBuilder |> addElement (BeginTable(true, aggrHeader))
        !> "Starting Duplicate detection report generation using file %s" request.SourceFileName
        let mutable count = 0
        while not reader.EndOfData do
            let record = reader.ReadFields()
            stats.TotalRecords <- stats.TotalRecords + 1
            let (reportRes, aggrResult) = performDedupe (record, headers, request, stats)
            builder.AppendLine(reportRes) |> ignore
            aggrBuilder.AppendLine(aggrResult) |> ignore
            count <- count + 1
            if count % 1000 = 0 then
                !> "Dedupe report: Completed %i" count
        !> "Dedupe report: Completed %i" count
        !> "Generating report"
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
             +/ (sprintf "%s_%s_%i.html" request.ProfileName (Path.GetFileNameWithoutExtension(request.SourceFileName)) 
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
                if File.Exists(body.SourceFileName) then 
                    let jobId = Guid.NewGuid()
                    requestProcessor.Post(jobId, body)
                    return jobId
                else return! fail <| ResourceNotFound(body.SourceFileName, "File")
            | _ -> return! fail <| UnknownSearchProfile(indexName, body.ProfileName)
        }
    
    override __.Process(request, body) = 
        body.Value.IndexName <- request.ResId.Value
        SomeResponse(processRequest request.ResId.Value body.Value, Ok, BadRequest)
