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
open Microsoft.Owin.Testing
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
          Server : TestServer }
    
    /// <summary>
    /// Build a new http test request
    /// </summary>
    /// <param name="httpMethod"></param>
    /// <param name="uri"></param>
    /// <param name="server"></param>
    let request (httpMethod : string) (uri : string) (server : TestServer) = 
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
        index.Fields <- index.Fields |> Array.append [|new Field(fieldName)|]
    

    // ----------------------------------------------------------------------------
    // Test assertions for FlexClient based tests
    // ----------------------------------------------------------------------------
    let hasHttpStatusCode expected (result, httpCode) = 
        if httpCode <> expected then printfn "%A" result
        httpCode =? expected
    let hasErrorCode expected (response : Response<_>) = 
        if isNull response || isNull response.Error then
            failwithf "Was expecting an error but received: %A" response
        else
            response.Error.ErrorCode =? expected
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


type ``Index Creation Tests``() = 

//    member __.``Accessing server root should return 200`` () = 
//        owinServer()
//        |> request "GET" "/"
//        |> execute
//        |> responseStatusEquals HttpStatusCode.OK
    
    [<Example("post-indices-id-1", "Creating an index without any data")>]
    member __.``Creating an index without any parameters should return 200`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.AddIndex(newIndex indexName).Result
        actual |> isCreated
        client.DeleteIndex(indexName).Result |> isSuccessful
    
    [<Example("post-indices-id-2", "Duplicate index cannot be created")>]
    member __.``Duplicate index cannot be created`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        let actual = client.AddIndex(index).Result
        actual |> fst |> hasErrorCode "IndexAlreadyExists"
        actual |> hasHttpStatusCode HttpStatusCode.Conflict
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    member __.``Create response contains the id of the created index`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        let actual = client.AddIndex(index).Result
        actual |> isCreated
        (actual |> data).Id =? index.IndexName
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    member __.``Index cannot be created without IndexName`` (client : FlexClient, handler : LoggingHandler) = 
        let actual = client.AddIndex(newIndex "").Result
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest
        
    [<Example("post-indices-id-3", "")>]
    member __.``Create index with two field 'firstname' & 'lastname'`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let index = newIndex indexName
        index.Fields <- [| new Field("firstname"); new Field("lastname")|]
        client.AddIndex(index).Result |> isCreated
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
    member __.``Create an index by setting all properties`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        let actual = client.AddIndex(index).Result
        actual |> hasHttpStatusCode HttpStatusCode.Created
        client.DeleteIndex(index.IndexName).Result |> isSuccessful

type ``Index Update Tests``() = 
    [<Example("put-indices-id-1", "")>]
    member __.``Trying to update an index is not supported`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        let actual = client.UpdateIndex(index).Result
        actual  |> fst |> hasErrorCode "HttpNotSupported"
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest

type ``Delete Index Test 1``() = 
    [<Example("delete-indices-id-1", "")>]
    member __.``Delete an index by id`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        client.DeleteIndex(index.IndexName).Result |> isSuccessful

type ``Delete Index Test 2``() =     
    [<Example("delete-indices-id-2", "")>]
    member __.``Trying to delete an non existing index will return error`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.DeleteIndex(indexName).Result
        actual  |> fst |> hasErrorCode "IndexNotFound"
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest

type ``Get Index Tests``() = 
    [<Example("get-indices-id-1", "")>]
    member __.``Getting an index detail by name`` (client : FlexClient, handler : LoggingHandler) = 
        let actual = client.GetIndex("contact").Result
        actual |> isSuccessful
        (actual |> data).IndexName =? "contact"
        actual |> hasHttpStatusCode HttpStatusCode.OK

type ``Get Non existing Index Tests``() =     
    [<Example("get-indices-id-2", "")>]
    member __.``Getting an non existing index will return error`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.GetIndex(indexName).Result
        actual  |> fst |> hasErrorCode "IndexNotFound"
        actual |> hasHttpStatusCode HttpStatusCode.NotFound

type ``Index Other Services Tests``() = 
    [<Example("get-indices-id-status-1", "Get status of an index (offine)")>]
    member __.``Newly created index is always offline`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        index.Active <- false
        client.AddIndex(index).Result |> isCreated
        let actual = client.GetIndexStatus(index.IndexName).Result
        actual |> isSuccessful
        (actual |> data).Status =? IndexStatus.Offline
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("put-indices-id-status-1", "")>]
    member __.``Set status of an index 'online'`` (client : FlexClient, index : Index, handler : LoggingHandler) = 
        index.Active <- false
        client.AddIndex(index).Result |> isCreated
        client.BringIndexOnline(index.IndexName).Result |> isSuccessful
        let actual = client.GetIndexStatus(index.IndexName).Result
        (actual |> data).Status =? IndexStatus.Online
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("put-indices-id-status-1", "")>]
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
        (actual |> data).Exists =? true
    
    [<Example("get-indices-1", "")>]
    member __.``Get all indices`` (client : FlexClient, handler : LoggingHandler) = 
        let actual = client.GetAllIndex().Result
        // Should have at least contact index
        (actual |> data).Count() >=? 1


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
        (actual |> data).RecordsReturned =? 10
    
    [<Example("post-indices-id-documents-id-2", "")>]
    member __.``Add a document to an index`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = createDocument client indexName
        actual |> fst |> isCreated
        (actual |> fst |> data).Id =? "1"

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
        actual |> isSuccessful
        
    [<Example("get-indices-id-documents-id-1", "")>]
    member __.``Get a document from an index`` (client : FlexClient, indexService : IIndexService, indexName : string, handler : LoggingHandler, documentService : IDocumentService) = 
        createDocument client indexName |> fst |> isCreated
        indexService.Refresh(indexName) |> isSuccessChoice

        let actual = client.GetDocument(indexName, "1").Result

        actual |> isSuccessful
        (actual |> data).Id =? "1"
        (actual |> data).Fields.["firstname"] =? "Seemant"

    [<Example("get-indices-id-documents-id-2", "")>]
    member __.``Non existing document should return Not found`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        createDocument client indexName |> fst |> isCreated
        let actual = client.GetDocument(indexName, "2").Result
        actual |> hasHttpStatusCode HttpStatusCode.NotFound

type ``Demo index Test``() =
    member __.``Setting up the demo index creates the country index``(client : FlexClient, handler : LoggingHandler) =
        client.SetupDemo().Result |> isSuccessful
        client.GetIndex("country").Result |> isSuccessful

type ``Search Tests``() = 
    
    //let indexData = Container.Resolve<FlexSearch.Core.Services.DemoIndexService>().DemoData().Value
    
    let Query (queryString : string) (recordsReturned : int) (available : int) (client : FlexClient) = 
        let searchQuery = new SearchQuery("country", queryString)
        searchQuery.Count <- 300
        searchQuery.Columns <- [|"countryname"; "agriproducts"; "governmenttype"; "population" |]
        let response = client.Search(searchQuery).Result
        response |> isSuccessful
        (response |> data).Documents
        |> Seq.iter 
               (fun x -> 
               printfn "Country Name:%s Agri products:%s Government type:%s" x.Fields.["countryname"] 
                   x.Fields.["agriproducts"] x.Fields.["governmenttype"])
        (response |> data).RecordsReturned =? recordsReturned
    
    [<Example("post-indices-search-term-1", "Term search using '=' operator")>]
    member __.``Term Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
        client |> Query "agriproducts = 'rice' and agriproducts = 'wheat'" expected 1
    
    [<Example("post-indices-search-term-2", "Term search using multiple words")>]
    member __.``Term Query Test 2`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
        client |> Query "agriproducts = 'rice wheat'" expected 1
    
    [<Example("post-indices-search-term-3", "Term search using '=' operator")>]
    member __.``Term Query Test 3`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
        client |> Query "agriproducts eq 'rice' or agriproducts eq 'wheat'" expected 1
    
    [<Example("post-indices-search-term-4", "Term search using '=' operator")>]
    member __.``Term Query Test 4`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
        client |> Query "agriproducts eq 'rice wheat' {clausetype : 'or'}" expected 1
    
    [<Example("post-indices-search-fuzzy-1", "Fuzzy search using 'fuzzy' operator")>]
    member __.``Fuzzy Query Test 1`` (client : FlexClient) = client |> Query "countryname fuzzy 'Iran'" 2 3
    
    [<Example("post-indices-search-fuzzy-2", "Fuzzy search using '~=' operator")>]
    member __.``Fuzzy Query Test 2`` (client : FlexClient) = client |> Query "countryname ~= 'Iran'" 2 3
    
    [<Example("post-indices-search-fuzzy-3", "Fuzzy search using slop parameter")>]
    member __.``Fuzzy Query Test 3`` (client : FlexClient) = client |> Query "countryname ~= 'China' {slop : '2'}" 3 3
    
    [<Example("post-indices-search-phrase-1", "Phrase search using match operator")>]
    member __.``Phrase Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.GovernmentType.Contains("federal parliamentary democracy")).Count()
        client |> Query "governmenttype match 'federal parliamentary democracy'" expected 4
    
    [<Example("post-indices-search-phrase-2", "Phrase search with slop of 4")>]
    member __.``Phrase Query Test 2`` (client : FlexClient) = 
        client |> Query "governmenttype match 'parliamentary monarchy' {slop : '4'}" 6 4
    
    [<Example("post-indices-search-phrase-3", "Phrase search with slop of 4")>]
    member __.``Phrase Query Test 3`` (client : FlexClient) = 
        client |> Query "governmenttype match 'monarchy parliamentary' {slop : '4'}" 3 4
    
    [<Example("post-indices-search-wildcard-1", "Wildcard search using 'like' operator")>]
    member __.``Wildcard Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni"))
        client |> Query "countryname like '*uni*'" (expected.Count()) 3
    
    [<Example("post-indices-search-wildcard-2", "Wildcard search using '%=' operator")>]
    member __.``Wildcard Query Test 2`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni")).Count()
        client |> Query "countryname %= '*uni*'" expected 3
    
    [<Example("post-indices-search-wildcard-3", "Wildcard search with single character operator")>]
    member __.``Wildcard Query Test 3`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> 
                     System.Text.RegularExpressions.Regex.Match(x.CountryName.ToLowerInvariant(), "unit[a-z]?d").Success)
                     .Count()
        client |> Query "countryname %= 'Unit?d'" expected 1
    
    [<Example("post-indices-search-regex-1", "Regex search using regex operator")>]
    member __.``Regex Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> 
                     System.Text.RegularExpressions.Regex.Match(x.AgriProducts.ToLowerInvariant(), "[ms]ilk").Success)
                     .Count()
        client |> Query "agriproducts regex '[ms]ilk'" expected 3
    
    [<Example("post-indices-search-matchall-1", "Match all search using 'matchall' operator")>]
    member __.``Matchall Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Count()
        client |> Query "countryname matchall '*'" expected 50
    
    [<Example("post-indices-search-range-1", "Greater than '>' operator")>]
    member __.``NumericRange Query Test 1`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population > 1000000L).Count()
        client |> Query "population > '1000000'" expected 48
    
    [<Example("post-indices-search-range-2", "Greater than or equal to '>=' operator")>]
    member __.``NumericRange Query Test 2`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population >= 1000000L).Count()
        client |> Query "population >= '1000000'" expected 48
    
    [<Example("post-indices-search-range-3", "Smaller than '<' operator")>]
    member __.``NumericRange Query Test 3`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population < 1000000L).Count()
        client |> Query "population < '1000000'" expected 48
    
    [<Example("post-indices-search-range-4", "Smaller than or equal to '<=' operator")>]
    member __.``NumericRange Query Test 4`` (client : FlexClient, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population <= 1000000L).Count()
        client |> Query "population <= '1000000'" expected 48
    
    [<Example("post-indices-search-highlighting-1", "Text highlighting example")>]
    member __.``Search Highlight Feature Test1`` (client : FlexClient) = 
        let query = new SearchQuery("country", "background = 'most prosperous countries'")
        let highlight = new List<string>()
        highlight.Add("background")
        query.Highlights <- new HighlightOption(highlight |> Seq.toArray)
        query.Highlights.FragmentsToReturn <- 2
        query.Columns <- [|"country"; "background"|]
        let result = client.Search(query).Result
        result |> isSuccessful
        (result |> data).Documents.Count >=? 0