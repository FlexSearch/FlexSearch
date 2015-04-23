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
namespace FlexSearch.DuplicateDetection

open FlexSearch.Core
open Microsoft.Owin
open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations.Schema
open System.Data.Entity
open System.Globalization
open System.Linq
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
    
    override this.Header 
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
    
    override this.LineItems 
        with get () = lineItems
        and set (v) = lineItems <- v
    
    member val SessionId = Unchecked.defaultof<int> with get, set
    abstract Session : Session with get, set
    
    override this.Session 
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
    
    override this.Headers 
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
        let addToColumns item (query : SearchQuery.Dto) = query.Columns <- query.Columns |> Array.append [|item|]
        let parallelOptions = new ParallelOptions(MaxDegreeOfParallelism = session.ThreadCount)
        let mainQuery = "_lastmodified > '20130101'"
//                sprintf "%s > '%s' AND %s < '%s'" session.DateTimeField 
//                    (session.RangeStartTime.ToString("yyyyMMddHHmmssfff")) session.DateTimeField 
//                    (session.RangeEndTime.ToString("yyyyMMddHHmmssfff"))
        let mainQuery = new SearchQuery.Dto(session.IndexName, mainQuery, Count = (int32) System.Int16.MaxValue)
        // TODO: Future optimization: bring only the required columns
        mainQuery |> addToColumns "*"
        match searchService.SearchAsDictionarySeq(mainQuery) with
        | Choice1Of2(records, recordsReturned, totalAvailable) -> 
            let secondaryQuery = new SearchQuery.Dto(session.IndexName, String.Empty, SearchProfile = session.ProfileName)
            session.RecordsReturned <- recordsReturned
            session.RecordsAvailable <- totalAvailable
            secondaryQuery |> addToColumns session.DisplayFieldName
            use context = new DataContext(connectionString)
            let session = context.Sessions.Add(session)
            context.SaveChanges() |> ignore
            Parallel.ForEach
                (records, parallelOptions, fun record -> DuplicateRecordCheck(record, secondaryQuery, session)) 
            |> ignore
            session.JobEndTime <- DateTime.Now
            context.SaveChanges() |> ignore
        | _ -> ()
        
    
    let requestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (jobId, session) = inbox.Receive()
                    PerformDuplicateDetection(jobId, session) |> ignore
                    return! loop()
                }
            loop())
    
    let processRequest(indexName : string, searchProfile : string, context : IOwinContext) = 
        let getDateTimeValueFromQueryString key (owin : IOwinContext) = 
            match owin.Request.Query.Get(key) with
            | null -> fail <| Error.MissingFieldValue(key)
            | value -> 
                match Int64.TryParse(value) with
                | true, v' -> ok(v')
                | _ -> fail <| Error.DataCannotBeParsed(key,"int64")
        maybe { 
            let! startDate = getDateTimeValueFromQueryString "startdate" context
            let! endDate = getDateTimeValueFromQueryString "enddate" context
            let dateTimeField = getValueFromQueryString "datetimefield" Constants.LastModifiedField context
            let displayName = getValueFromQueryString "displayname" Constants.IdField context
            let threadCount = getIntValueFromQueryString "threadcount" 1 context
            let! indexWriter = indexService.IsIndexOnline indexName
            
            match indexWriter.Settings.SearchProfiles.TryGetValue(searchProfile) with
            | true, profile -> 
                let session = new Session()
                session.IndexName <- indexName
                session.ProfileName <- searchProfile
                session.RangeStartTime <- DateTime.ParseExact
                                              (startDate.ToString(), [| "yyyyMMdd"; "yyyyMMddHHmm"; "yyyyMMddHHmmss" |], 
                                               CultureInfo.InvariantCulture, DateTimeStyles.None)
                session.RangeEndTime <- DateTime.ParseExact
                                            (endDate.ToString(), [| "yyyyMMdd"; "yyyyMMddHHmm"; "yyyyMMddHHmmss" |], 
                                             CultureInfo.InvariantCulture, DateTimeStyles.None)
                session.DisplayFieldName <- displayName
                session.DateTimeField <- dateTimeField
                session.JobStartTime <- DateTime.Now
                session.ThreadCount <- threadCount
                let jobId = Guid.NewGuid()
                requestProcessor.Post(jobId, session)
                return! ok(jobId)
            | _ -> 
                return! fail <| Error.UnknownSearchProfile(indexName,searchProfile)
        }
    
    override this.Process(request, body) = 
        SomeResponse(processRequest(request.ResId.Value, request.SubResId.Value, request.OwinContext), Ok, BadRequest)
