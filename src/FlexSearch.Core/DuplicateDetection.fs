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
open System.Threading.Tasks

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

[<Sealed>]
[<Name("POST-/indices/:id/duplicatedetection/:id")>]
type SqlHandler(indexService : IIndexService, searchService : ISearchService) = 
    inherit HttpHandlerBase<NoBody, Guid>()
    let connectionString = "Data Source=(localdb)\\v11.0;Integrated Security=True"
    
    let DuplicateRecordCheck(record : Dictionary<string, string>, query : SearchQuery.Dto, session : Session) = 
        match searchService.SearchUsingProfile(query, record) with
        | Choice1Of2(results) -> 
            if results.RecordsReturned > 1 then 
                use context = new DataContext(connectionString)
                let header = 
                    context.headers.Add
                        (new Header(PrimaryRecordId = record.[Constants.IdField], SessionId = session.SessionId, 
                                    DisplayName = record.[session.DisplayFieldName]))
                for result in results.Documents do
                    // The returned record is same as the passed record. Save the score for relative
                    // scoring later
                    if result.Id = header.PrimaryRecordId then header.Score <- result.Score
                    else 
                        let score = 
                            if header.Score >= result.Score then result.Score / header.Score * 100.0
                            else -1.0
                        context.LineItems.Add
                            (new LineItem(SecondaryRecordId = result.Id, 
                                          DisplayName = result.Fields.[session.DisplayFieldName], Score = score, 
                                          HeaderId = header.HeaderId)) |> ignore
                context.SaveChanges() |> ignore
        | _ -> ()
    
    let PerformDuplicateDetection(jobId, session : Session) = 
        maybe { 
            let parallelOptions = new ParallelOptions(MaxDegreeOfParallelism = session.ThreadCount)
            let mainQuery = 
                sprintf "%s > '%i' AND %s < '%i'" session.DateTimeField (session.RangeStartTime |> dateToFlexFormat) 
                    session.DateTimeField (session.RangeEndTime |> dateToFlexFormat)
            let mainQuery = new SearchQuery.Dto(session.IndexName, mainQuery, Count = (int32) System.Int16.MaxValue)
            // TODO: Future optimization: bring only the required columns
            mainQuery.Columns <- [| "*" |]
            let! (records, recordsReturned, totalAvailable) = searchService.SearchAsDictionarySeq(mainQuery)
            let secondaryQuery = 
                new SearchQuery.Dto(session.IndexName, String.Empty, SearchProfile = session.ProfileName)
            session.RecordsReturned <- recordsReturned
            session.RecordsAvailable <- totalAvailable
            secondaryQuery.Columns <- [| session.DisplayFieldName |]
            use context = new DataContext(connectionString)
            let session = context.Sessions.Add(session)
            context.SaveChanges() |> ignore
            Parallel.ForEach
                (records, parallelOptions, fun record -> DuplicateRecordCheck(record, secondaryQuery, session)) 
            |> ignore
            session.JobEndTime <- DateTime.Now
            context.SaveChanges() |> ignore
        }
    
    let requestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (jobId, session) = inbox.Receive()
                    PerformDuplicateDetection(jobId, session) |> ignore
                    return! loop()
                }
            loop())
    
    override __.Process(request, _) = 
        match indexService.IsIndexOnline(request.ResId.Value) with
        | Choice1Of2(writer) -> 
            let startDate = request.OwinContext |> longFromQueryString "startdate" (DateTime.Now |> dateToFlexFormat)
            let endDate = 
                request.OwinContext |> longFromQueryString "enddate" (DateTime.Now.AddDays(-1.0) |> dateToFlexFormat)
            let dateTimeField = request.OwinContext |> stringFromQueryString "datetimefield" Constants.LastModifiedField
            let displayName = request.OwinContext |> stringFromQueryString "displayname" Constants.IdField
            let threadCount = request.OwinContext |> intFromQueryString "threadcount" 1
            match writer.Settings.SearchProfiles.TryGetValue(request.SubResId.Value) with
            | true, _ -> 
                let session = new Session()
                session.IndexName <- request.ResId.Value
                session.ProfileName <- request.SubResId.Value
                session.RangeStartTime <- parseDate <| startDate.ToString()
                session.RangeEndTime <- parseDate <| endDate.ToString()
                session.DisplayFieldName <- displayName
                session.DateTimeField <- dateTimeField
                session.JobStartTime <- DateTime.Now
                session.ThreadCount <- threadCount
                let jobId = Guid.NewGuid()
                requestProcessor.Post(jobId, session)
                SuccessResponse(jobId, Ok)
            | _ -> FailureResponse(UnknownSearchProfile(request.ResId.Value, request.SubResId.Value), BadRequest)
        | Choice2Of2(error) -> FailureResponse(error, BadRequest)
