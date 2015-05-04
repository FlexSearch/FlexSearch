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
open System.IO
open System.Text
open System.Threading.Tasks
open System.Web.UI

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

type FileWriterCommands = 
    | BeginTable of record : Dictionary<string, string>
    | AddRecord of record : Dictionary<string, string>
    | EndTable

[<Sealed>]
[<Name("POST-/indices/:id/duplicatedetection/:id")>]
type SqlHandler(indexService : IIndexService, searchService : ISearchService) = 
    inherit HttpHandlerBase<DuplicateDetectionRequest, Guid>()
    
    let writeSessionBeginInfo (req : DuplicateDetectionRequest, session) = 
        use context = new DataContext(req.ConnectionString)
        let session = context.Sessions.Add(session)
        context.SaveChanges() |> ignore
        session
    
    /// Compile the complete report
    let generateReport (session : Session, req : DuplicateDetectionRequest) = 
        let mutable template = File.ReadAllText(WebFolder +/ "Reports//DuplicateDetectionTemplate.html")
        let body = File.ReadAllText(req.ReportName)
        template <- template.Replace("{{IndexName}}", req.IndexName)
        template <- template.Replace("{{ProfileName}}", req.ProfileName)
        template <- template.Replace("{{StartRange}}", req.StartDate.ToString())
        template <- template.Replace("{{EndRange}}", req.EndDate.ToString())
        template <- template.Replace("{{TotalChecked}}", session.RecordsReturned.ToString())
        template <- template.Replace("{{TotalDuplicates}}", "")
        template <- template.Replace("{{Results}}", body)
        File.WriteAllText(sprintf "%s.html" req.ReportName, template)
        File.Delete(req.ReportName)
    
    let writeSessionEndInfo (req : DuplicateDetectionRequest, session : Session) = 
        use context = new DataContext(req.ConnectionString)
        context.SaveChanges() |> ignore
        if req.GenerateReport then generateReport (session, req)
    
    let fileWriter = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let builder = new StringBuilder()
                    let! (command : FileWriterCommands, filePath) = inbox.Receive()
                    builder.Clear() |> ignore
                    match command with
                    | BeginTable(record) -> 
                        builder.Append("""<table class="table"><thead><tr>""") |> ignore
                        for pair in record do
                            builder.Append(sprintf "<th>%s</th>" pair.Value) |> ignore
                        builder.Append("""</tr></thead><tbody>""") |> ignore
                    | AddRecord(record) -> 
                        builder.Append("<tr>") |> ignore
                        for pair in record do
                            builder.Append(sprintf "<td>%s</td>" pair.Value) |> ignore
                        builder.Append("</tr>") |> ignore
                    | EndTable -> builder.Append("</tbody></table>") |> ignore
                    File.AppendAllText(filePath, builder.ToString())
                    return! loop()
                }
            loop())
    
    let duplicateRecordCheck (req : DuplicateDetectionRequest, record : Dictionary<string, string>, session : Session) = 
        let query = new SearchQuery.Dto(session.IndexName, String.Empty, SearchProfile = session.ProfileName)
        if req.GenerateReport then query.Columns <- [| "*" |]
        else query.Columns <- [| session.DisplayFieldName |]
        query.ReturnFlatResult <- true
        query.ReturnScore <- true
        match searchService.Search(query, record) with
        | Choice1Of2(results) -> 
            if results.Meta.RecordsReturned > 1 then 
                use context = new DataContext(req.ConnectionString)
                let header = 
                    context.headers.Add
                        (new Header(PrimaryRecordId = record.[Constants.IdField], SessionId = session.SessionId, 
                                    DisplayName = record.[session.DisplayFieldName]))
                if req.GenerateReport then 
                    fileWriter.Post(BeginTable record, req.ReportName)
                    fileWriter.Post(AddRecord record, req.ReportName)
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
                        if req.GenerateReport then fileWriter.Post(AddRecord result, req.ReportName)
                if req.GenerateReport then fileWriter.Post(EndTable, req.ReportName)
                context.SaveChanges() |> ignore
        | _ -> ()
    
    let performDuplicateDetection (jobId, indexWriter : IndexWriter.T, req : DuplicateDetectionRequest) = 
        maybe { 
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
            let dateTimeField = indexWriter.GetSchemaName(session.DateTimeField)
            let mainQuery = 
                sprintf "%s > '%i' AND %s < '%i'" dateTimeField (session.RangeStartTime |> dateToFlexFormat) 
                    dateTimeField (session.RangeEndTime |> dateToFlexFormat)
            let mainQuery = new SearchQuery.Dto(session.IndexName, mainQuery, Count = (int32) System.Int16.MaxValue)
            // TODO: Future optimization: bring only the required columns
            mainQuery.Columns <- [| "*" |]
            let! result = searchService.Search(mainQuery)
            let records = result |> toFlatResults
            session.RecordsReturned <- result.Meta.RecordsReturned
            session.RecordsAvailable <- result.Meta.TotalAvailable
            let session = writeSessionBeginInfo (req, session)
            Parallel.ForEach
                (records.Documents, parallelOptions, fun record -> duplicateRecordCheck (req, record, session)) 
            |> ignore
            session.JobEndTime <- DateTime.Now
            writeSessionEndInfo (req, session)
        }
    
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
                                    |> longFromQueryString "startdate" (DateTime.Now |> dateToFlexFormat)
            body.Value.EndDate <- request.OwinContext 
                                  |> longFromQueryString "enddate" (DateTime.Now.AddDays(-1.0) |> dateToFlexFormat)
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
                requestProcessor.Post(jobId, writer, body.Value)
                SuccessResponse(jobId, Ok)
            | _ -> FailureResponse(UnknownSearchProfile(request.ResId.Value, request.SubResId.Value), BadRequest)
        | Choice2Of2(error) -> FailureResponse(error, BadRequest)
