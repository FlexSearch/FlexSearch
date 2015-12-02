module IntegrationTests.Rest

open Autofac
open FlexSearch.Core
open Client
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Text
open System.Threading
open Swensen.Unquote

[<AutoOpenAttribute>]
module Helpers = 
    open FlexSearch.Core
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    open System
    open System.Collections.Generic
    open System.IO
    open System.Linq
    open System.Net
    open System.Text
    open System.Threading
    
    // ----------------------------------------------------------------------------
    // Test request pattern
    // ----------------------------------------------------------------------------
    type RequestBuilder = 
        { RequestType : string
          Uri : string
          mutable RequestBody : string
          mutable Response : HttpResponseMessage
          Server : WebServer }
    
    /// <summary>
    /// Build a new http test request
    /// </summary>
    /// <param name="httpMethod"></param>
    /// <param name="uri"></param>
    /// <param name="server"></param>
    let request (httpMethod : string) (uri : string) (server : WebServer) = 
        let request = 
            { RequestType = httpMethod
              Uri = uri
              RequestBody = ""
              Response = null
              Server = server }
        request
    
    let withBody (body : string) (requestBuilder : RequestBuilder) = 
        requestBuilder.RequestBody <- body
        requestBuilder
    
    let execute (requestBuilder : RequestBuilder) = 
        match requestBuilder.RequestType with
        | "GET" -> requestBuilder.Response <- requestBuilder.Server.HttpClient.GetAsync(requestBuilder.Uri).Result
        | "POST" -> 
            let content = new StringContent(requestBuilder.RequestBody, Encoding.UTF8, "application/json")
            requestBuilder.Response <- requestBuilder.Server.HttpClient.PostAsync(requestBuilder.Uri, content).Result
        | "PUT" -> 
            let content = new StringContent(requestBuilder.RequestBody, Encoding.UTF8, "application/json")
            requestBuilder.Response <- requestBuilder.Server.HttpClient.PutAsync(requestBuilder.Uri, content).Result
        | "DELETE" -> requestBuilder.Response <- requestBuilder.Server.HttpClient.DeleteAsync(requestBuilder.Uri).Result
        | _ -> failwithf "Not supported"
        requestBuilder
    
    let newIndex indexName = new Index(IndexName = indexName)
    let addField (index : Index) (fieldName : string) = 
        index.Fields <- index.Fields |> Array.append [| new Field(fieldName) |]
    
    // ----------------------------------------------------------------------------
    // Test assertions for FlexClient based tests
    // ----------------------------------------------------------------------------
    let hasHttpStatusCode expected (result, httpCode) = 
        if httpCode <> expected then printfn "%A" result
        httpCode =? expected
    
    let hasErrorCode expected (response : Response<_>) = 
        if isNull response || isNull response.Error then failwithf "Was expecting an error but received: %A" response
        else response.Error.ErrorCode =? expected
    
    let isSuccessful response = response |> hasHttpStatusCode HttpStatusCode.OK
    let isCreated response = response |> hasHttpStatusCode HttpStatusCode.Created
    let responseStatusEquals status result = result.Response.StatusCode =? status
    let data (response : Response<_> * HttpStatusCode) = (response |> fst).Data
    
    let isSuccessChoice choice = 
        match choice with
        | Ok(_) -> true
        | Fail(error) -> 
            printfn "Error: %A" error
            false
        =? true
    
    type ResultLog() = 
        member val Result = Unchecked.defaultof<SearchResults> with get, set
        member val Query = Unchecked.defaultof<SearchQuery> with get, set
        member val Description = Unchecked.defaultof<string> with get, set
    
    let formatter = new NewtonsoftJsonFormatter() :> IFormatter

    /// Write the request details to the specified folder
    /// Force the JIT to not inline this method otherwise Stack frame will return the wrong method name
    [<System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>]
    let log (id : string) (client : LoggingHandler) = 
        if Global.RequestLogPath <> String.Empty && Directory.Exists(Global.RequestLogPath) then 
            let frame = new System.Diagnostics.StackFrame(1)
            let desc = frame.GetMethod().Name
            File.WriteAllText(Global.RequestLogPath +/ id + ".http", client.Log().ToString())
    
    /// Force the JIT to not inline this method otherwise Stack frame will return the wrong method name
    [<System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>]
    let query (queryString : string) (recordsReturned : int) (available : int) (client : FlexClient) = 
        let searchQuery = new SearchQuery("country", queryString)
        searchQuery.Count <- 10
        searchQuery.Columns <- [| "countryname"; "agriproducts"; "governmenttype"; "population" |]
        let response = client.Search(searchQuery).Result
        response |> isSuccessful
        (response |> data).TotalAvailable =? recordsReturned
        /// Log the result if log path is defined
        if Global.RequestLogPath <> String.Empty && Directory.Exists(Global.RequestLogPath) then 
            let frame = new System.Diagnostics.StackFrame(1)
            
            let meth = frame.GetMethod()
            match meth.CustomAttributes |> Seq.tryFind (fun x -> x.AttributeType = typeof<ExampleAttribute>) with
            | Some(attr) -> 
                let fileName = attr.ConstructorArguments.[0].ToString().Replace('"', ' ').Trim()

                let desc = 
                    if not <| String.IsNullOrWhiteSpace(attr.ConstructorArguments.[1].ToString()) then 
                        attr.ConstructorArguments.[1].ToString().Replace('"', ' ').Trim()
                    else meth.Name
                
                let result = new ResultLog()
                result.Query <- searchQuery
                result.Result <- response |> data
                result.Description <-desc
                File.WriteAllText(Global.RequestLogPath +/ fileName + ".json", formatter.SerializeToString(result))
            | None -> ()

type ``Index Creation Tests``() = 
    
    //    member __.``Accessing server root should return 200`` () = 
    //        owinServer()
    //        |> request "GET" "/"
    //        |> execute
    //        |> responseStatusEquals HttpStatusCode.OK
    [<Example("post-indices-id-1", "Creating an index without any data")>]
    member __.``Creating an index without any parameters should return 200`` (client : FlexClient, indexName : string, 
                                                                              handler : LoggingHandler) = 
        let actual = client.AddIndex(newIndex indexName).Result
        actual |> isCreated
        handler |> log "post-indices-id-1"
        client.DeleteIndex(indexName).Result |> isSuccessful
    
    [<Example("post-indices-id-2", "Duplicate index cannot be created")>]
    member __.``Duplicate index cannot be created`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        let actual = client.AddIndex(index).Result
        actual
        |> fst
        |> hasErrorCode "IndexAlreadyExists"
        actual |> hasHttpStatusCode HttpStatusCode.Conflict
        handler |> log "post-indices-id-2"
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    member __.``Create response contains the id of the created index`` (client : FlexClient, index : Index, 
                                                                        handler : LoggingHandler) = 
        let actual = client.AddIndex(index).Result
        actual |> isCreated
        (actual |> data).Id =? index.IndexName
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    member __.``Index cannot be created without IndexName`` (client : FlexClient, handler : LoggingHandler) = 
        let actual = client.AddIndex(newIndex"").Result
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest
    
    [<Example("post-indices-id-3", "")>]
    member __.``Create index with two field 'firstname' & 'lastname'`` (client : FlexClient, indexName : string, 
                                                                        handler : LoggingHandler) = 
        let index = newIndex indexName
        index.Fields <- [| new Field("firstname")
                           new Field("lastname") |]
        client.AddIndex(index).Result |> isCreated
        handler |> log "post-indices-id-3"
        client.DeleteIndex(indexName).Result |> isSuccessful
    
    //    [<Example("post-indices-id-4", "")>]
    //    member __.``Create an index with dynamic fields`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
    //        let index = newIndex indexName
    //        index.Fields <- [| new Field.Dto("firstname")
    //                           new Field.Dto("lastname")
    //                           new Field.Dto("fullname", ScriptName = "fullnamescript") |]
    //        index.Scripts <- 
    //            [| new Script.Dto(ScriptName = "fullnamescript", Source = "return fields.firstname + \" \" + fields.lastname;", ScriptType = ScriptType.Dto.ComputedField) |]
    //        client.AddIndex(index).Result |> isCreated
    //        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    [<Example("post-indices-id-5", "")>]
    member __.``Create an index by setting all properties`` (client : FlexClient, index : Index, 
                                                             handler : LoggingHandler) = 
        let actual = client.AddIndex(index).Result
        actual |> hasHttpStatusCode HttpStatusCode.Created
        handler |> log "post-indices-id-5"
        client.DeleteIndex(index.IndexName).Result |> isSuccessful

type ``Index Update Tests``() = 
    
    [<Example("put-indices-id-1", "")>]
    member __.``Trying to update an index is not supported`` (client : FlexClient, index : Index, 
                                                              handler : LoggingHandler) = 
        let actual = client.UpdateIndex(index).Result
        actual
        |> fst
        |> hasErrorCode "HttpNotSupported"
        handler |> log "put-indices-id-1"
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest
    
    [<Example("put-indices-id-2", "")>]
    member __.``Trying to update index fields should return success`` (client : FlexClient, index : Index, 
                                                                       handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        let fields = new FieldsUpdateRequest(Fields = [| new Field("et1", FieldDataType.Text, Store = true) |])
        isSuccessful <| client.UpdateIndexFields(index.IndexName, fields).Result
        handler |> log "put-indices-id-2"
    
    [<Example("put-indices-id-3", "")>]
    member __.``Trying to update index search profile should return success`` (client : FlexClient, index : Index, 
                                                                               handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        let sp = new SearchQuery(index.IndexName, "et1 matchall 'x'", QueryName = "all")
        isSuccessful <| client.UpdateIndexSearchProfile(index.IndexName, sp).Result
        handler |> log "put-indices-id-3"
    
    [<Example("put-indices-id-4", "")>]
    member __.``Trying to update index configuration should return success`` (client : FlexClient, index : Index, 
                                                                              handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        let conf = new IndexConfiguration(CommitTimeSeconds = 100)
        isSuccessful <| client.UpdateIndexConfiguration(index.IndexName, conf).Result
        handler |> log "put-indices-id-4"

type ``Delete Index Test 1``() = 
    [<Example("delete-indices-id-1", "")>]
    member __.``Delete an index by id`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
        handler |> log "delete-indices-id-1"

type ``Delete Index Test 2``() = 
    [<Example("delete-indices-id-2", "")>]
    member __.``Trying to delete an non existing index will return error`` (client : FlexClient, indexName : string, 
                                                                            handler : LoggingHandler) = 
        let actual = client.DeleteIndex(indexName).Result
        actual
        |> fst
        |> hasErrorCode "IndexNotFound"
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest
        handler |> log "delete-indices-id-2"

type ``Get Index Tests``() = 
    [<Example("get-indices-id-1", "")>]
    member __.``Getting an index detail by name`` (client : FlexClient, handler : LoggingHandler) = 
        let actual = client.GetIndex("contact").Result
        actual |> isSuccessful
        (actual |> data).IndexName =? "contact"
        actual |> hasHttpStatusCode HttpStatusCode.OK
        handler |> log "get-indices-id-1"

type ``Get Non existing Index Tests``() = 
    [<Example("get-indices-id-2", "")>]
    member __.``Getting an non existing index will return error`` (client : FlexClient, indexName : string, 
                                                                   handler : LoggingHandler) = 
        let actual = client.GetIndex(indexName).Result
        actual
        |> fst
        |> hasErrorCode "IndexNotFound"
        actual |> hasHttpStatusCode HttpStatusCode.NotFound
        handler |> log "get-indices-id-2"

type ``Index Other Services Tests``() = 
    
    [<Example("get-indices-id-status-1", "Get status of an index (offine)")>]
    member __.``Newly created index is always offline`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        index.Active <- false
        client.AddIndex(index).Result |> isCreated
        let actual = client.GetIndexStatus(index.IndexName).Result
        actual |> isSuccessful
        (actual |> data).Status =? IndexStatus.Offline
        handler |> log "get-indices-id-status-1"
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("put-indices-id-status-1", "")>]
    member __.``Set status of an index 'online'`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        index.Active <- false
        client.AddIndex(index).Result |> isCreated
        client.BringIndexOnline(index.IndexName).Result |> isSuccessful
        let actual = client.GetIndexStatus(index.IndexName).Result
        (actual |> data).Status =? IndexStatus.Online
        handler |> log "put-indices-id-status-1"
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    member __.``Set status of an index 'offline'`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        let actual = client.GetIndexStatus(index.IndexName).Result
        actual |> isSuccessful
        (actual |> data).Status =? IndexStatus.Online
        client.SetIndexOffline(index.IndexName).Result |> isSuccessful
        let actual = client.GetIndexStatus(index.IndexName).Result
        (actual |> data).Status =? IndexStatus.Offline
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("get-indices-id-exists-1", "")>]
    member __.``Check if a given index exists`` (client : FlexClient, indexName : Guid, handler : LoggingHandler) = 
        let actual = client.IndexExists("contact").Result
        actual |> isSuccessful
        handler |> log "get-indices-id-exists-1"
        (actual |> data).Exists =? true
    
    [<Example("get-indices-1", "")>]
    member __.``Get all indices`` (client : FlexClient, handler : LoggingHandler) = 
        let actual = client.GetAllIndex().Result
        handler |> log "get-indices-1"
        (// Should have at least contact index
         actual |> data).Count() >=? 1

type ``Document Tests``() = 
    
    let testIndex indexName = 
        let index = new Index(IndexName = indexName, Active = true)
        index.Fields <- [| new Field("firstname", FieldDataType.Text)
                           new Field("lastname") |]
        index
    
    let createDocument (client : FlexClient) indexName = 
        client.AddIndex(testIndex indexName).Result |> isCreated
        let document = new Document(indexName, "1")
        document.Fields.Add("firstname", "Seemant")
        document.Fields.Add("lastname", "Rajvanshi")
        let result = client.AddDocument(indexName, document).Result
        (result, document)
    
    [<Example("get-indices-id-documents-1", "")>]
    member __.``Get top 10 documents from an index`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.GetTopDocuments("country", 10).Result
        actual |> isSuccessful
        handler |> log "get-indices-id-documents-1"
        (actual |> data).RecordsReturned =? 10
    
    [<Example("post-indices-id-documents-id-2", "")>]
    member __.``Add a document to an index`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = createDocument client indexName
        actual
        |> fst
        |> isCreated
        handler |> log "post-indices-id-documents-id-2"
        (actual
         |> fst
         |> data).Id
        =? "1"
    
    member __.``Cannot add a document without an id`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        client.AddIndex(testIndex indexName).Result |> isCreated
        let document = new Document(indexName, " ")
        client.AddDocument(indexName, document).Result |> hasHttpStatusCode HttpStatusCode.BadRequest
        printfn "%s" (handler.Log().ToString())
    
    [<Example("put-indices-id-documents-id-1", "")>]
    member __.``Update a document to an index`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        // Create the document
        let (result, document) = createDocument client indexName
        result |> isCreated
        // Update the document
        document.Fields.["lastname"] <- "Rajvanshi1"
        let actual = client.UpdateDocument(indexName, document).Result
        handler |> log "put-indices-id-documents-id-2"
        actual |> isSuccessful
    
    [<Example("get-indices-id-documents-id-1", "")>]
    member __.``Get a document from an index`` (client : FlexClient, indexService : IIndexService, indexName : string, 
                                                handler : LoggingHandler, documentService : IDocumentService) = 
        createDocument client indexName
        |> fst
        |> isCreated
        indexService.Refresh(indexName) |> isSuccessChoice
        let actual = client.GetDocument(indexName, "1").Result
        actual |> isSuccessful
        (actual |> data).Id =? "1"
        (actual |> data).Fields.["firstname"] =? "Seemant"
        handler |> log "get-indices-id-documents-id-1"
    
    [<Example("get-indices-id-documents-id-2", "")>]
    member __.``Non existing document should return Not found`` (client : FlexClient, indexName : string, 
                                                                 handler : LoggingHandler) = 
        createDocument client indexName
        |> fst
        |> isCreated
        let actual = client.GetDocument(indexName, "2").Result
        actual |> hasHttpStatusCode HttpStatusCode.NotFound
        handler |> log "get-indices-id-documents-id-2"

type ``Demo index Test``() = 
    member __.``Setting up the demo index creates the country index`` (client : FlexClient, handler : LoggingHandler) = 
        client.SetupDemo().Result |> isSuccessful
        client.GetIndex("country").Result |> isSuccessful

type ``Search Tests``() = 
    
    [<Example("post-indices-search-term-1", "Term search using '=' operator")>]
    member __.``Term Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
        client |> query "agriproducts = 'rice' and agriproducts = 'wheat'" expected 1
    
    [<Example("post-indices-search-term-2", "Term search using multiple words")>]
    member __.``Term Query Test 2`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
        client |> query "agriproducts = 'rice wheat'" expected 1
    
    [<Example("post-indices-search-term-3", "Term search using '=' operator")>]
    member __.``Term Query Test 3`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
        client |> query "agriproducts eq 'rice' or agriproducts eq 'wheat'" expected 1
    
    [<Example("post-indices-search-term-4", "Term search using '=' operator")>]
    member __.``Term Query Test 4`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
        client |> query "agriproducts eq 'rice wheat' {clausetype : 'or'}" expected 1
    
    [<Example("post-indices-search-fuzzy-1", "Fuzzy search using 'fuzzy' operator")>]
    member __.``Fuzzy Query Test 1`` (client : FlexClient) = client |> query "countryname fuzzy 'Iran'" 2 3
    
    [<Example("post-indices-search-fuzzy-2", "Fuzzy search using '~=' operator")>]
    member __.``Fuzzy Query Test 2`` (client : FlexClient) = client |> query "countryname ~= 'Iran'" 2 3
    
    [<Example("post-indices-search-fuzzy-3", "Fuzzy search using slop parameter")>]
    member __.``Fuzzy Query Test 3`` (client : FlexClient) = client |> query "countryname ~= 'China' {slop : '2'}" 3 3
    
    [<Example("post-indices-search-phrase-1", "Phrase search using match operator")>]
    member __.``Phrase Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.GovernmentType.Contains("federal parliamentary democracy")).Count()
        client |> query "governmenttype match 'federal parliamentary democracy'" expected 4
    
    [<Example("post-indices-search-phrase-2", "Phrase search with slop of 4")>]
    member __.``Phrase Query Test 2`` (client : FlexClient) = 
        client |> query "governmenttype match 'parliamentary monarchy' {slop : '4'}" 6 4
    
    [<Example("post-indices-search-phrase-3", "Phrase search with slop of 4")>]
    member __.``Phrase Query Test 3`` (client : FlexClient) = 
        client |> query "governmenttype match 'monarchy parliamentary' {slop : '4'}" 3 4
    
    [<Example("post-indices-search-wildcard-1", "Wildcard search using 'like' operator")>]
    member __.``Wildcard Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni"))
        client |> query "countryname like '*uni*'" (expected.Count()) 3
    
    [<Example("post-indices-search-wildcard-2", "Wildcard search using '%=' operator")>]
    member __.``Wildcard Query Test 2`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni")).Count()
        client |> query "countryname %= '*uni*'" expected 3
    
    [<Example("post-indices-search-wildcard-3", "Wildcard search with single character operator")>]
    member __.``Wildcard Query Test 3`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> 
                     System.Text.RegularExpressions.Regex.Match(x.CountryName.ToLowerInvariant(), "unit[a-z]?d").Success)
                     .Count()
        client |> query "countryname %= 'Unit?d'" expected 1
    
    [<Example("post-indices-search-regex-1", "Regex search using regex operator")>]
    member __.``Regex Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> 
                     System.Text.RegularExpressions.Regex.Match(x.AgriProducts.ToLowerInvariant(), "[ms]ilk").Success)
                     .Count()
        client |> query "agriproducts regex '[ms]ilk'" expected 3
    
    [<Example("post-indices-search-matchall-1", "Match all search using 'matchall' operator")>]
    member __.``Matchall Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Count()
        client |> query "countryname matchall '*'" expected 50
    
    [<Example("post-indices-search-range-1", "Greater than '>' operator")>]
    member __.``NumericRange Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population > 1000000L).Count()
        client |> query "population > '1000000'" expected 48
    
    [<Example("post-indices-search-range-2", "Greater than or equal to '>=' operator")>]
    member __.``NumericRange Query Test 2`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population >= 1000000L).Count()
        client |> query "population >= '1000000'" expected 48
    
    [<Example("post-indices-search-range-3", "Smaller than '<' operator")>]
    member __.``NumericRange Query Test 3`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population < 1000000L).Count()
        client |> query "population < '1000000'" expected 48
    
    [<Example("post-indices-search-range-4", "Smaller than or equal to '<=' operator")>]
    member __.``NumericRange Query Test 4`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population <= 1000000L).Count()
        client |> query "population <= '1000000'" expected 48
    
    [<Example("post-indices-search-highlighting-1", "Text highlighting example")>]
    member __.``Search Highlight Feature Test1`` (client : FlexClient) = 
        let query = new SearchQuery("country", "background = 'most prosperous countries'")
        let highlight = new List<string>()
        highlight.Add("background")
        query.Highlights <- new HighlightOption(highlight |> Seq.toArray)
        query.Highlights.FragmentsToReturn <- 2
        query.Columns <- [| "country"; "background" |]
        let result = client.Search(query).Result
        result |> isSuccessful
        (result |> data).Documents.Count >=? 0
