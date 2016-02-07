namespace FlexSearch.HttpTests

open FlexSearch.Api.Api
open FlexSearch.Api.Model
open FlexSearch.Api.Client
open FlexSearch.Api.Constants
open ResponseLogging
open Global
open TestCommandHelpers
open Swensen.Unquote
open System.Net
open System

type ``Index Creation Tests``() = 
    
    [<Example("post-indices-id-1", "Creating an index without any data")>]
    member __.``Creating an index without any parameters should return 200`` (api : IndicesApi, indexName : string, 
                                                                              handler : LoggingHandler) = 
        api.CreateIndexWithHttpInfo(newIndex indexName) |> isCreated
        handler |> log "post-indices-id-1"
        api.DeleteIndex(indexName) |> isSuccessful
    
    [<Example("post-indices-id-2", "Duplicate index cannot be created")>]
    member __.``Duplicate index cannot be created`` (api : IndicesApi, index : Index, handler : LoggingHandler) = 
        api.CreateIndex(index) |> isSuccessful
        api.CreateIndexWithHttpInfo(index)
        |> hasStatusCode HttpStatusCode.Conflict
        |> hasApiErrorCode "IndexAlreadyExists"
        |> ignore
        handler |> log "post-indices-id-2"
        api.DeleteIndex(index.IndexName) |> isSuccessful
        
    member __.``Create response contains the id of the created index`` (api : IndicesApi, index : Index, 
                                                                        handler : LoggingHandler) = 
        let actual = api.CreateIndexWithHttpInfo(index)
        actual |> isCreated
        actual.Data.Data.Id =? index.IndexName
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    member __.``Index cannot be created without IndexName`` (api : IndicesApi, handler : LoggingHandler) = 
        api.CreateIndexWithHttpInfo(newIndex "") 
        |> hasStatusCode HttpStatusCode.BadRequest
        |> ignore
    
    [<Example("post-indices-id-3", "")>]
    member __.``Create index with two field 'firstname' & 'lastname'`` (api : IndicesApi, indexName : string, 
                                                                        handler : LoggingHandler) = 
        let index = newIndex indexName
        index.Fields <- [| new Field("firstname")
                           new Field("lastname") |]
        api.CreateIndex(index) |> isSuccessful
        handler |> log "post-indices-id-3"
        api.DeleteIndex(indexName) |> isSuccessful
    
    //    [<Example("post-indices-id-4", "")>]
    //    member __.``Create an index with dynamic fields`` (api : IndicesApi, indexName : string, handler : LoggingHandler) = 
    //        let index = newIndex indexName
    //        index.Fields <- [| new Field.Dto("firstname")
    //                           new Field.Dto("lastname")
    //                           new Field.Dto("fullname", ScriptName = "fullnamescript") |]
    //        index.Scripts <- 
    //            [| new Script.Dto(ScriptName = "fullnamescript", Source = "return fields.firstname + \" \" + fields.lastname;", ScriptType = ScriptType.Dto.ComputedField) |]
    //        api.CreateIndex(index).Result |> isCreated
    //        api.DeleteIndex(index.IndexName).Result |> isSuccessful
    [<Example("post-indices-id-5", "")>]
    member __.``Create an index by setting all properties`` (api : IndicesApi, index : Index, 
                                                             handler : LoggingHandler) = 
        api.CreateIndexWithHttpInfo(index)
        |> hasStatusCode HttpStatusCode.Created
        |> ignore
        handler |> log "post-indices-id-5"
        api.DeleteIndex(index.IndexName) |> isSuccessful

type ``Index Update Tests``() = 
    
//    [<Example("put-indices-id-1", "")>]
//    member __.``Trying to update an index is not supported`` (api : IndicesApi, index : Index, 
//                                                              handler : LoggingHandler) = 
//        let actual = api.UpdateIndex(index).Result
//        actual
//        |> fst
//        |> hasErrorCode "HttpNotSupported"
//        handler |> log "put-indices-id-1"
//        actual |> hasStatusCode HttpStatusCode.BadRequest
    
    [<Example("put-indices-id-2", "")>]
    member __.``Trying to update index fields should return success`` (api : IndicesApi, index : Index, 
                                                                       handler : LoggingHandler) = 
        api.CreateIndexWithHttpInfo(index) |> isCreated
        let fields = new FieldsUpdateRequest(Fields = [| new Field("et1", FieldType.Text) |])
        api.UpdateIndexFields(fields, index.IndexName) |> isSuccessful
        handler |> log "put-indices-id-2"
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    [<Example("put-indices-id-3", "")>]
    member __.``Trying to update index search profile should return success`` (api : IndicesApi, index : Index, 
                                                                               handler : LoggingHandler) = 
        api.CreateIndexWithHttpInfo(index) |> isCreated
        let sp = new SearchQuery(index.IndexName, "et1 matchall 'x'", QueryName = "all")
        api.UpdateIndexPredefinedQuery(sp, index.IndexName) |> isSuccessful
        handler |> log "put-indices-id-3"
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    [<Example("put-indices-id-4", "")>]
    member __.``Trying to update index configuration should return success`` (api : IndicesApi, index : Index, 
                                                                              handler : LoggingHandler) = 
        api.CreateIndexWithHttpInfo(index) |> isCreated
        let conf = new IndexConfiguration(CommitTimeSeconds = 100)
        api.UpdateIndexConfiguration(conf, index.IndexName) |> isSuccessful
        handler |> log "put-indices-id-4"
        api.DeleteIndex(index.IndexName) |> isSuccessful

type ``Delete Index Tests``() = 
    [<Example("delete-indices-id-1", "")>]
    member __.``Delete an index by id`` (api : IndicesApi, index : Index, handler : LoggingHandler) = 
        api.CreateIndexWithHttpInfo(index) |> isCreated
        api.DeleteIndex(index.IndexName) |> isSuccessful
        handler |> log "delete-indices-id-1"

    [<Example("delete-indices-id-2", "")>]
    member __.``Trying to delete an non existing index will return error`` (api : IndicesApi, indexName : string, 
                                                                            handler : LoggingHandler) = 
        api.DeleteIndexWithHttpInfo(indexName)
        |> hasApiErrorCode "IndexNotFound"
        |> hasStatusCode HttpStatusCode.BadRequest
        |> ignore
        handler |> log "delete-indices-id-2"

type ``Get Index Tests``() = 
    [<Example("get-indices-id-1", "")>]
    member __.``Getting an index detail by name`` (api : IndicesApi, handler : LoggingHandler) = 
        let actual = api.GetIndexWithHttpInfo("contact")
        actual.Data |> isSuccessful
        actual.Data.Data.IndexName =? "contact"
        actual |> hasStatusCode HttpStatusCode.OK |> ignore
        handler |> log "get-indices-id-1"

type ``Get Non existing Index Tests``() = 
    [<Example("get-indices-id-2", "")>]
    member __.``Getting an non existing index will return error`` (api : IndicesApi, indexName : string, 
                                                                   handler : LoggingHandler) = 
        api.GetIndexWithHttpInfo(indexName)
        |> hasApiErrorCode "IndexNotFound"
        |> hasStatusCode HttpStatusCode.NotFound
        |> ignore
        handler |> log "get-indices-id-2"

type ``Index Other Services Tests``() = 
    
    [<Example("get-indices-id-status-1", "Get status of an index (offine)")>]
    member __.``Newly created index is always offline`` (api : IndicesApi, index : Index, handler : LoggingHandler) = 
        index.Active <- false
        api.CreateIndexWithHttpInfo(index) |> isCreated
        let actual = api.GetStatus(index.IndexName)
        actual |> isSuccessful
        actual.Data.IndexStatus =? IndexStatus.Offline
        handler |> log "get-indices-id-status-1"
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    [<Example("put-indices-id-status-1", "")>]
    member __.``Set status of an index 'online'`` (api : IndicesApi, index : Index, handler : LoggingHandler) = 
        index.Active <- false
        api.CreateIndex(index) |> isSuccessful
        api.UpdateStatus(index.IndexName, "online") |> isSuccessful
        api.GetStatus(index.IndexName).Data.IndexStatus =? IndexStatus.Online
        handler |> log "put-indices-id-status-1"
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    member __.``Set status of an index 'offline'`` (api : IndicesApi, index : Index, handler : LoggingHandler) = 
        api.CreateIndex(index) |> isSuccessful
        api.GetStatus(index.IndexName)
        |> fun r -> r.Data.IndexStatus =? IndexStatus.Online; r
        |> isSuccessful

        api.UpdateStatus(index.IndexName, "offline") |> isSuccessful

        api.GetStatus(index.IndexName).Data.IndexStatus =? IndexStatus.Offline
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    [<Example("get-indices-id-exists-1", "")>]
    member __.``Check if a given index exists`` (api : IndicesApi, handler : LoggingHandler) = 
        api.IndexExists("contact")
        |> fun r -> r.Data.Exists =? true; r
        |> isSuccessful
        handler |> log "get-indices-id-exists-1"

    member __.``Checking if a given index exists should not return error if index not present`` (api : IndicesApi, handler : LoggingHandler) = 
        let actual = api.IndexExists("index-which-does-not-exist")
        actual |> isSuccessful
        actual.Data.Exists =? false
        handler |> log "get-indices-id-exists-1"

    
    [<Example("get-indices-1", "")>]
    member __.``Get all indices`` (api : IndicesApi, handler : LoggingHandler) = 
        api.GetAllIndex()
        // Should have at least contact index
        |> fun r -> r.Data.Length >=? 1
        handler |> log "get-indices-1"

type ``Document Tests``(demoApi : ServerApi) = 
    
    let testIndex indexName = 
        let index = new Index(IndexName = indexName, Active = true)
        index.Fields <- [| new Field("firstname", FlexSearch.Api.Constants.FieldType.Text)
                           new Field("lastname") |]
        index
    
    let createDocument (api : CommonApi) indexName = 
        api.CreateIndexWithHttpInfo(testIndex indexName) |> isCreated
        let document = new Document(indexName = indexName, id = "1")
        document.Fields.Add("firstname", "Seemant")
        document.Fields.Add("lastname", "Rajvanshi")
        let result = api.CreateDocument(document, indexName)
        (result, document)
    
    do demoApi.SetupDemo() |> isSuccessful

    [<Example("get-indices-id-documents-1", "")>]
    member __.``Get top 10 documents from an index`` (api : DocumentsApi, indexName : string, handler : LoggingHandler) = 
        let actual = api.GetDocuments("country")
        actual |> isSuccessful
        handler |> log "get-indices-id-documents-1"
        actual.Data.RecordsReturned =? 10
    
    [<Example("post-indices-id-documents-id-2", "")>]
    member __.``Add a document to an index`` (api : CommonApi, indexName : string, handler : LoggingHandler) = 
        let actual = createDocument api indexName
        actual |> fst |> isSuccessful
        handler |> log "post-indices-id-documents-id-2"
        (actual |> fst).Data.Id =? "1"
        api.DeleteIndex(indexName) |> isSuccessful
    
    member __.``Cannot add a document without an id`` (api : CommonApi, indexName : string, handler : LoggingHandler) = 
        api.CreateIndexWithHttpInfo(testIndex indexName) |> isCreated
        let document = new Document(indexName = indexName, id = "")
        api.CreateDocumentWithHttpInfo(document, indexName) 
        |> hasStatusCode HttpStatusCode.BadRequest
        |> ignore
        printfn "%s" (handler.Log().ToString())
        api.DeleteIndex(indexName) |> isSuccessful
    
    [<Example("put-indices-id-documents-id-1", "")>]
    member __.``Update a document to an index`` (api : CommonApi, indexName : string, handler : LoggingHandler) = 
        // Create the document
        let (result, document) = createDocument api indexName
        result |> isSuccessful
        // Update the document
        document.Fields.["lastname"] <- "Rajvanshi1"
        let actual = api.UpdateDocument(document, indexName, document.Id)
        handler |> log "put-indices-id-documents-id-2"
        actual |> isSuccessful
        api.DeleteIndex(indexName) |> isSuccessful
    
    [<Example("get-indices-id-documents-id-1", "")>]
    member __.``Get a document from an index`` (api : DocumentsApi, handler : LoggingHandler) = 
        let actual = api.GetDocument("country", "1")
        actual |> isSuccessful
        actual.Data.Id =? "1"
        actual.Data.Fields.["countryname"] =? "Afghanistan"
        handler |> log "get-indices-id-documents-id-1"
    
    [<Example("get-indices-id-documents-id-2", "")>]
    member __.``Non existing document should return Not found`` (api : CommonApi, indexName : string, 
                                                                 handler : LoggingHandler) = 
        createDocument api indexName
        |> fst |> isSuccessful
        api.GetDocumentWithHttpInfo(indexName, "2")
        |> hasStatusCode HttpStatusCode.NotFound
        |> ignore
        handler |> log "get-indices-id-documents-id-2"
        api.DeleteIndex(indexName) |> isSuccessful

type ``Demo index Test``() = 
    member __.``Setting up the demo index creates the country index`` (serverApi : ServerApi, indicesApi : IndicesApi, handler : LoggingHandler) = 
        serverApi.SetupDemo() |> isSuccessful
        indicesApi.GetIndex("country") |> isSuccessful

// An alias so that we avoid opening FlexSearch.Core
type Country = FlexSearch.Core.Country

open System.Linq

type ``Search Tests``() = 
    [<Example("post-indices-search-term-1", "Term search using '=' operator")>]
    member __.``Term Query Test 1`` (api : SearchApi, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
        api |> query "agriproducts = 'rice' and agriproducts = 'wheat'" expected 1
    
    [<Example("post-indices-search-term-2", "Term search using multiple words")>]
    member __.``Term Query Test 2`` (api : SearchApi, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
        api |> query "agriproducts = 'rice wheat'" expected 1
    
    [<Example("post-indices-search-term-3", "Term search using '=' operator")>]
    member __.``Term Query Test 3`` (api : SearchApi, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
        api |> query "agriproducts eq 'rice' or agriproducts eq 'wheat'" expected 1
    
    [<Example("post-indices-search-term-4", "Term search using '=' operator")>]
    member __.``Term Query Test 4`` (api : SearchApi, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
        api |> query "agriproducts eq 'rice wheat' {clausetype : 'or'}" expected 1
    
    [<Example("post-indices-search-fuzzy-1", "Fuzzy search using 'fuzzy' operator")>]
    member __.``Fuzzy Query Test 1`` (api : SearchApi) = api |> query "countryname fuzzy 'Iran'" 2 3
    
    [<Example("post-indices-search-fuzzy-2", "Fuzzy search using '~=' operator")>]
    member __.``Fuzzy Query Test 2`` (api : SearchApi) = api |> query "countryname ~= 'Iran'" 2 3
    
    [<Example("post-indices-search-fuzzy-3", "Fuzzy search using slop parameter")>]
    member __.``Fuzzy Query Test 3`` (api : SearchApi) = api |> query "countryname ~= 'China' {slop : '2'}" 3 3
    
    [<Example("post-indices-search-phrase-1", "Phrase search using match operator")>]
    member __.``Phrase Query Test 1`` (api : SearchApi, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.GovernmentType.Contains("federal parliamentary democracy")).Count()
        api |> query "governmenttype match 'federal parliamentary democracy'" expected 4
    
    [<Example("post-indices-search-phrase-2", "Phrase search with slop of 4")>]
    member __.``Phrase Query Test 2`` (api : SearchApi) = 
        api |> query "governmenttype match 'parliamentary monarchy' {slop : '4'}" 6 4
    
    [<Example("post-indices-search-phrase-3", "Phrase search with slop of 4")>]
    member __.``Phrase Query Test 3`` (api : SearchApi) = 
        api |> query "governmenttype match 'monarchy parliamentary' {slop : '4'}" 3 4
    
    [<Example("post-indices-search-wildcard-1", "Wildcard search using 'like' operator")>]
    member __.``Wildcard Query Test 1`` (api : SearchApi, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni"))
        api |> query "countryname like '*uni*'" (expected.Count()) 3
    
    [<Example("post-indices-search-wildcard-2", "Wildcard search using '%=' operator")>]
    member __.``Wildcard Query Test 2`` (api : SearchApi, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni")).Count()
        api |> query "countryname %= '*uni*'" expected 3
    
    [<Example("post-indices-search-wildcard-3", "Wildcard search with single character operator")>]
    member __.``Wildcard Query Test 3`` (api : SearchApi, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> 
                     System.Text.RegularExpressions.Regex.Match(x.CountryName.ToLowerInvariant(), "unit[a-z]?d").Success)
                     .Count()
        api |> query "countryname %= 'Unit?d'" expected 1
    
    [<Example("post-indices-search-regex-1", "Regex search using regex operator")>]
    member __.``Regex Query Test 1`` (api : SearchApi, indexData : Country list) = 
        let expected = 
            indexData.Where(fun x -> 
                     System.Text.RegularExpressions.Regex.Match(x.AgriProducts.ToLowerInvariant(), "[ms]ilk").Success)
                     .Count()
        api |> query "agriproducts regex '[ms]ilk'" expected 3
    
    [<Example("post-indices-search-matchall-1", "Match all search using 'matchall' operator")>]
    member __.``Matchall Query Test 1`` (api : SearchApi, indexData : Country list) = 
        let expected = indexData.Count()
        api |> query "countryname matchall '*'" expected 50
    
    [<Example("post-indices-search-range-1", "Greater than '>' operator")>]
    member __.``NumericRange Query Test 1`` (api : SearchApi, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population > 1000000L).Count()
        api |> query "population > '1000000'" expected 48
    
    [<Example("post-indices-search-range-2", "Greater than or equal to '>=' operator")>]
    member __.``NumericRange Query Test 2`` (api : SearchApi, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population >= 1000000L).Count()
        api |> query "population >= '1000000'" expected 48
    
    [<Example("post-indices-search-range-3", "Smaller than '<' operator")>]
    member __.``NumericRange Query Test 3`` (api : SearchApi, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population < 1000000L).Count()
        api |> query "population < '1000000'" expected 48
    
    [<Example("post-indices-search-range-4", "Smaller than or equal to '<=' operator")>]
    member __.``NumericRange Query Test 4`` (api : SearchApi, indexData : Country list) = 
        let expected = indexData.Where(fun x -> x.Population <= 1000000L).Count()
        api |> query "population <= '1000000'" expected 48
    
    [<Example("post-indices-search-highlighting-1", "Text highlighting example")>]
    member __.``Search Highlight Feature Test1`` (api : SearchApi) = 
        let query = new SearchQuery("country", "background = 'most prosperous countries'")
        let highlight = [| "background" |]
        query.Highlights <- new HighlightOption(highlight)
        query.Highlights.FragmentsToReturn <- 2
        query.Columns <- [| "country"; "background" |]
        api.PostSearch(query, query.IndexName)
        |> fun r -> r |> isSuccessful; r
        |> fun r -> r.Data.Documents.Length >=? 0
