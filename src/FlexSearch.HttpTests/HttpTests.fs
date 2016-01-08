namespace FlexSearch.HttpTests

open FlexSearch.Api.Api
open FlexSearch.Api.Model
open FlexSearch.Api.Client
open ResponseLogging
open Global
open TestCommandHelpers
open Swensen.Unquote

type ``Index Creation Tests``() = 
    
    //    member __.``Accessing server root should return 200`` () = 
    //        owinServer()
    //        |> request "GET" "/"
    //        |> execute
    //        |> responseStatusEquals HttpStatusCode.OK
    [<Example("post-indices-id-1", "Creating an index without any data")>]
    member __.``Creating an index without any parameters should return 200`` (api : IndicesApi, indexName : string, 
                                                                              handler : LoggingHandler) = 
        let actual = api.CreateIndex(newIndex indexName)
        test <@ actual.Data.Id |> isNotNullOrEmpty @>
        handler |> log "post-indices-id-1"
        api.DeleteIndex(indexName)
    
    [<Example("post-indices-id-2", "Duplicate index cannot be created")>]
    member __.``Duplicate index cannot be created`` (api : IndicesApi, index : Index, handler : LoggingHandler) = 
        api.CreateIndex(index) |> ignore
        try api.CreateIndex(index) |> ignore
        with :? ApiException as e -> e.ErrorCode =? 1
//        actual
//        |> fst
//        |> hasErrorCode "IndexAlreadyExists"
//        actual |> hasHttpStatusCode HttpStatusCode.Conflict
        handler |> log "post-indices-id-2"
        api.DeleteIndex(index.IndexName)
    
//    member __.``Create response contains the id of the created index`` (api : ApiClient, index : Index, 
//                                                                        handler : LoggingHandler) = 
//        let actual = api.CreateIndex(index).Result
//        actual |> isCreated
//        (actual |> data).Id =? index.IndexName
//        api.DeleteIndex(index.IndexName).Result |> isSuccessful
//    
//    member __.``Index cannot be created without IndexName`` (api : ApiClient, handler : LoggingHandler) = 
//        let actual = api.CreateIndex(newIndex"").Result
//        actual |> hasHttpStatusCode HttpStatusCode.BadRequest
//    
//    [<Example("post-indices-id-3", "")>]
//    member __.``Create index with two field 'firstname' & 'lastname'`` (api : ApiClient, indexName : string, 
//                                                                        handler : LoggingHandler) = 
//        let index = newIndex indexName
//        index.Fields <- [| new Field("firstname")
//                           new Field("lastname") |]
//        api.CreateIndex(index).Result |> isCreated
//        handler |> log "post-indices-id-3"
//        api.DeleteIndex(indexName).Result |> isSuccessful
//    
//    //    [<Example("post-indices-id-4", "")>]
//    //    member __.``Create an index with dynamic fields`` (api : ApiClient, indexName : string, handler : LoggingHandler) = 
//    //        let index = newIndex indexName
//    //        index.Fields <- [| new Field.Dto("firstname")
//    //                           new Field.Dto("lastname")
//    //                           new Field.Dto("fullname", ScriptName = "fullnamescript") |]
//    //        index.Scripts <- 
//    //            [| new Script.Dto(ScriptName = "fullnamescript", Source = "return fields.firstname + \" \" + fields.lastname;", ScriptType = ScriptType.Dto.ComputedField) |]
//    //        api.CreateIndex(index).Result |> isCreated
//    //        api.DeleteIndex(index.IndexName).Result |> isSuccessful
//    [<Example("post-indices-id-5", "")>]
//    member __.``Create an index by setting all properties`` (api : ApiClient, index : Index, 
//                                                             handler : LoggingHandler) = 
//        let actual = api.CreateIndex(index).Result
//        actual |> hasHttpStatusCode HttpStatusCode.Created
//        handler |> log "post-indices-id-5"
//        api.DeleteIndex(index.IndexName).Result |> isSuccessful
//
//type ``Index Update Tests``() = 
//    
//    [<Example("put-indices-id-1", "")>]
//    member __.``Trying to update an index is not supported`` (api : ApiClient, index : Index, 
//                                                              handler : LoggingHandler) = 
//        let actual = api.UpdateIndex(index).Result
//        actual
//        |> fst
//        |> hasErrorCode "HttpNotSupported"
//        handler |> log "put-indices-id-1"
//        actual |> hasHttpStatusCode HttpStatusCode.BadRequest
//    
////    [<Example("put-indices-id-2", "")>]
////    member __.``Trying to update index fields should return success`` (api : ApiClient, index : Index, 
////                                                                       handler : LoggingHandler) = 
////        api.CreateIndex(index).Result |> isCreated
////        let fields = new FieldsUpdateRequest(Fields = [| new Field("et1", FieldDataType.Text, Store = true) |])
////        isSuccessful <| api.UpdateIndexFields(index.IndexName, fields).Result
////        handler |> log "put-indices-id-2"
//    
//    [<Example("put-indices-id-3", "")>]
//    member __.``Trying to update index search profile should return success`` (api : ApiClient, index : Index, 
//                                                                               handler : LoggingHandler) = 
//        api.CreateIndex(index).Result |> isCreated
//        let sp = new SearchQuery(index.IndexName, "et1 matchall 'x'", QueryName = "all")
//        isSuccessful <| api.UpdateIndexSearchProfile(index.IndexName, sp).Result
//        handler |> log "put-indices-id-3"
//    
//    [<Example("put-indices-id-4", "")>]
//    member __.``Trying to update index configuration should return success`` (api : ApiClient, index : Index, 
//                                                                              handler : LoggingHandler) = 
//        api.CreateIndex(index).Result |> isCreated
//        let conf = new IndexConfiguration(CommitTimeSeconds = 100)
//        isSuccessful <| api.UpdateIndexConfiguration(index.IndexName, conf).Result
//        handler |> log "put-indices-id-4"
//
//type ``Delete Index Test 1``() = 
//    [<Example("delete-indices-id-1", "")>]
//    member __.``Delete an index by id`` (api : ApiClient, index : Index, handler : LoggingHandler) = 
//        api.CreateIndex(index).Result |> isCreated
//        api.DeleteIndex(index.IndexName).Result |> isSuccessful
//        handler |> log "delete-indices-id-1"
//
//type ``Delete Index Test 2``() = 
//    [<Example("delete-indices-id-2", "")>]
//    member __.``Trying to delete an non existing index will return error`` (api : ApiClient, indexName : string, 
//                                                                            handler : LoggingHandler) = 
//        let actual = api.DeleteIndex(indexName).Result
//        actual
//        |> fst
//        |> hasErrorCode "IndexNotFound"
//        actual |> hasHttpStatusCode HttpStatusCode.BadRequest
//        handler |> log "delete-indices-id-2"
//
//type ``Get Index Tests``() = 
//    [<Example("get-indices-id-1", "")>]
//    member __.``Getting an index detail by name`` (api : ApiClient, handler : LoggingHandler) = 
//        let actual = api.GetIndex("contact").Result
//        actual |> isSuccessful
//        (actual |> data).IndexName =? "contact"
//        actual |> hasHttpStatusCode HttpStatusCode.OK
//        handler |> log "get-indices-id-1"
//
//type ``Get Non existing Index Tests``() = 
//    [<Example("get-indices-id-2", "")>]
//    member __.``Getting an non existing index will return error`` (api : ApiClient, indexName : string, 
//                                                                   handler : LoggingHandler) = 
//        let actual = api.GetIndex(indexName).Result
//        actual
//        |> fst
//        |> hasErrorCode "IndexNotFound"
//        actual |> hasHttpStatusCode HttpStatusCode.NotFound
//        handler |> log "get-indices-id-2"
//
//type ``Index Other Services Tests``() = 
//    
//    [<Example("get-indices-id-status-1", "Get status of an index (offine)")>]
//    member __.``Newly created index is always offline`` (api : ApiClient, index : Index, handler : LoggingHandler) = 
//        index.Active <- false
//        api.CreateIndex(index).Result |> isCreated
//        let actual = api.GetIndexStatus(index.IndexName).Result
//        actual |> isSuccessful
//        (actual |> data).IndexStatus =? IndexStatus.Offline
//        handler |> log "get-indices-id-status-1"
//        api.DeleteIndex(index.IndexName).Result |> isSuccessful
//    
//    [<Example("put-indices-id-status-1", "")>]
//    member __.``Set status of an index 'online'`` (api : ApiClient, index : Index, handler : LoggingHandler) = 
//        index.Active <- false
//        api.CreateIndex(index).Result |> isCreated
//        api.BringIndexOnline(index.IndexName).Result |> isSuccessful
//        let actual = api.GetIndexStatus(index.IndexName).Result
//        (actual |> data).IndexStatus =? IndexStatus.Online
//        handler |> log "put-indices-id-status-1"
//        api.DeleteIndex(index.IndexName).Result |> isSuccessful
//    
//    member __.``Set status of an index 'offline'`` (api : ApiClient, index : Index, handler : LoggingHandler) = 
//        api.CreateIndex(index).Result |> isCreated
//        let actual = api.GetIndexStatus(index.IndexName).Result
//        actual |> isSuccessful
//        (actual |> data).IndexStatus =? IndexStatus.Online
//        api.SetIndexOffline(index.IndexName).Result |> isSuccessful
//        let actual = api.GetIndexStatus(index.IndexName).Result
//        (actual |> data).IndexStatus =? IndexStatus.Offline
//        api.DeleteIndex(index.IndexName).Result |> isSuccessful
//    
//    [<Example("get-indices-id-exists-1", "")>]
//    member __.``Check if a given index exists`` (api : ApiClient, indexName : Guid, handler : LoggingHandler) = 
//        let actual = api.IndexExists("contact").Result
//        actual |> isSuccessful
//        handler |> log "get-indices-id-exists-1"
//        (actual |> data).Exists =? true
//    
//    [<Example("get-indices-1", "")>]
//    member __.``Get all indices`` (api : ApiClient, handler : LoggingHandler) = 
//        let actual = api.GetAllIndex().Result
//        handler |> log "get-indices-1"
//        (// Should have at least contact index
//         actual |> data).Count() >=? 1
//
//type ``Document Tests``() = 
//    
//    let testIndex indexName = 
//        let index = new Index(IndexName = indexName, Active = true)
//        index.Fields <- [| new Field("firstname", FlexSearch.Api.Constants.FieldType.Text)
//                           new Field("lastname") |]
//        index
//    
//    let createDocument (api : ApiClient) indexName = 
//        api.CreateIndex(testIndex indexName).Result |> isCreated
//        let document = new Document(indexName = indexName, id = "1")
//        document.Fields.Add("firstname", "Seemant")
//        document.Fields.Add("lastname", "Rajvanshi")
//        let result = api.AddDocument(indexName, document).Result
//        (result, document)
//    
//    [<Example("get-indices-id-documents-1", "")>]
//    member __.``Get top 10 documents from an index`` (api : ApiClient, indexName : string, handler : LoggingHandler) = 
//        let actual = api.GetTopDocuments("country", 10).Result
//        actual |> isSuccessful
//        handler |> log "get-indices-id-documents-1"
//        (actual |> data).RecordsReturned =? 10
//    
//    [<Example("post-indices-id-documents-id-2", "")>]
//    member __.``Add a document to an index`` (api : ApiClient, indexName : string, handler : LoggingHandler) = 
//        let actual = createDocument client indexName
//        actual
//        |> fst
//        |> isCreated
//        handler |> log "post-indices-id-documents-id-2"
//        (actual
//         |> fst
//         |> data).Id
//        =? "1"
//    
//    member __.``Cannot add a document without an id`` (api : ApiClient, indexName : string, handler : LoggingHandler) = 
//        api.CreateIndex(testIndex indexName).Result |> isCreated
//        let document = new Document(indexName = indexName, id = " ")
//        api.AddDocument(indexName, document).Result |> hasHttpStatusCode HttpStatusCode.BadRequest
//        printfn "%s" (handler.Log().ToString())
//    
//    [<Example("put-indices-id-documents-id-1", "")>]
//    member __.``Update a document to an index`` (api : ApiClient, indexName : string, handler : LoggingHandler) = 
//        // Create the document
//        let (result, document) = createDocument client indexName
//        result |> isCreated
//        // Update the document
//        document.Fields.["lastname"] <- "Rajvanshi1"
//        let actual = api.UpdateDocument(indexName, document).Result
//        handler |> log "put-indices-id-documents-id-2"
//        actual |> isSuccessful
//    
//    [<Example("get-indices-id-documents-id-1", "")>]
//    member __.``Get a document from an index`` (api : ApiClient, indexService : IIndexService, indexName : string, 
//                                                handler : LoggingHandler, documentService : IDocumentService) = 
//        createDocument client indexName
//        |> fst
//        |> isCreated
//        indexService.Refresh(indexName) |> isSuccessChoice
//        let actual = api.GetDocument(indexName, "1").Result
//        actual |> isSuccessful
//        (actual |> data).Id =? "1"
//        (actual |> data).Fields.["firstname"] =? "Seemant"
//        handler |> log "get-indices-id-documents-id-1"
//    
//    [<Example("get-indices-id-documents-id-2", "")>]
//    member __.``Non existing document should return Not found`` (api : ApiClient, indexName : string, 
//                                                                 handler : LoggingHandler) = 
//        createDocument client indexName
//        |> fst
//        |> isCreated
//        let actual = api.GetDocument(indexName, "2").Result
//        actual |> hasHttpStatusCode HttpStatusCode.NotFound
//        handler |> log "get-indices-id-documents-id-2"
//
//type ``Demo index Test``() = 
//    member __.``Setting up the demo index creates the country index`` (api : ApiClient, handler : LoggingHandler) = 
//        api.SetupDemo().Result |> isSuccessful
//        api.GetIndex("country").Result |> isSuccessful
//
//type ``Search Tests``() = 
//    
//    [<Example("post-indices-search-term-1", "Term search using '=' operator")>]
//    member __.``Term Query Test 1`` (api : ApiClient, indexData : Country list) = 
//        let expected = 
//            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
//        client |> query "agriproducts = 'rice' and agriproducts = 'wheat'" expected 1
//    
//    [<Example("post-indices-search-term-2", "Term search using multiple words")>]
//    member __.``Term Query Test 2`` (api : ApiClient, indexData : Country list) = 
//        let expected = 
//            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
//        client |> query "agriproducts = 'rice wheat'" expected 1
//    
//    [<Example("post-indices-search-term-3", "Term search using '=' operator")>]
//    member __.``Term Query Test 3`` (api : ApiClient, indexData : Country list) = 
//        let expected = 
//            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
//        client |> query "agriproducts eq 'rice' or agriproducts eq 'wheat'" expected 1
//    
//    [<Example("post-indices-search-term-4", "Term search using '=' operator")>]
//    member __.``Term Query Test 4`` (api : ApiClient, indexData : Country list) = 
//        let expected = 
//            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
//        client |> query "agriproducts eq 'rice wheat' {clausetype : 'or'}" expected 1
//    
//    [<Example("post-indices-search-fuzzy-1", "Fuzzy search using 'fuzzy' operator")>]
//    member __.``Fuzzy Query Test 1`` (api : ApiClient) = client |> query "countryname fuzzy 'Iran'" 2 3
//    
//    [<Example("post-indices-search-fuzzy-2", "Fuzzy search using '~=' operator")>]
//    member __.``Fuzzy Query Test 2`` (api : ApiClient) = client |> query "countryname ~= 'Iran'" 2 3
//    
//    [<Example("post-indices-search-fuzzy-3", "Fuzzy search using slop parameter")>]
//    member __.``Fuzzy Query Test 3`` (api : ApiClient) = client |> query "countryname ~= 'China' {slop : '2'}" 3 3
//    
//    [<Example("post-indices-search-phrase-1", "Phrase search using match operator")>]
//    member __.``Phrase Query Test 1`` (api : ApiClient, indexData : Country list) = 
//        let expected = indexData.Where(fun x -> x.GovernmentType.Contains("federal parliamentary democracy")).Count()
//        client |> query "governmenttype match 'federal parliamentary democracy'" expected 4
//    
//    [<Example("post-indices-search-phrase-2", "Phrase search with slop of 4")>]
//    member __.``Phrase Query Test 2`` (api : ApiClient) = 
//        client |> query "governmenttype match 'parliamentary monarchy' {slop : '4'}" 6 4
//    
//    [<Example("post-indices-search-phrase-3", "Phrase search with slop of 4")>]
//    member __.``Phrase Query Test 3`` (api : ApiClient) = 
//        client |> query "governmenttype match 'monarchy parliamentary' {slop : '4'}" 3 4
//    
//    [<Example("post-indices-search-wildcard-1", "Wildcard search using 'like' operator")>]
//    member __.``Wildcard Query Test 1`` (api : ApiClient, indexData : Country list) = 
//        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni"))
//        client |> query "countryname like '*uni*'" (expected.Count()) 3
//    
//    [<Example("post-indices-search-wildcard-2", "Wildcard search using '%=' operator")>]
//    member __.``Wildcard Query Test 2`` (api : ApiClient, indexData : Country list) = 
//        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni")).Count()
//        client |> query "countryname %= '*uni*'" expected 3
//    
//    [<Example("post-indices-search-wildcard-3", "Wildcard search with single character operator")>]
//    member __.``Wildcard Query Test 3`` (api : ApiClient, indexData : Country list) = 
//        let expected = 
//            indexData.Where(fun x -> 
//                     System.Text.RegularExpressions.Regex.Match(x.CountryName.ToLowerInvariant(), "unit[a-z]?d").Success)
//                     .Count()
//        client |> query "countryname %= 'Unit?d'" expected 1
//    
//    [<Example("post-indices-search-regex-1", "Regex search using regex operator")>]
//    member __.``Regex Query Test 1`` (api : ApiClient, indexData : Country list) = 
//        let expected = 
//            indexData.Where(fun x -> 
//                     System.Text.RegularExpressions.Regex.Match(x.AgriProducts.ToLowerInvariant(), "[ms]ilk").Success)
//                     .Count()
//        client |> query "agriproducts regex '[ms]ilk'" expected 3
//    
//    [<Example("post-indices-search-matchall-1", "Match all search using 'matchall' operator")>]
//    member __.``Matchall Query Test 1`` (api : ApiClient, indexData : Country list) = 
//        let expected = indexData.Count()
//        client |> query "countryname matchall '*'" expected 50
//    
//    [<Example("post-indices-search-range-1", "Greater than '>' operator")>]
//    member __.``NumericRange Query Test 1`` (api : ApiClient, indexData : Country list) = 
//        let expected = indexData.Where(fun x -> x.Population > 1000000L).Count()
//        client |> query "population > '1000000'" expected 48
//    
//    [<Example("post-indices-search-range-2", "Greater than or equal to '>=' operator")>]
//    member __.``NumericRange Query Test 2`` (api : ApiClient, indexData : Country list) = 
//        let expected = indexData.Where(fun x -> x.Population >= 1000000L).Count()
//        client |> query "population >= '1000000'" expected 48
//    
//    [<Example("post-indices-search-range-3", "Smaller than '<' operator")>]
//    member __.``NumericRange Query Test 3`` (api : ApiClient, indexData : Country list) = 
//        let expected = indexData.Where(fun x -> x.Population < 1000000L).Count()
//        client |> query "population < '1000000'" expected 48
//    
//    [<Example("post-indices-search-range-4", "Smaller than or equal to '<=' operator")>]
//    member __.``NumericRange Query Test 4`` (api : ApiClient, indexData : Country list) = 
//        let expected = indexData.Where(fun x -> x.Population <= 1000000L).Count()
//        client |> query "population <= '1000000'" expected 48
//    
//    [<Example("post-indices-search-highlighting-1", "Text highlighting example")>]
//    member __.``Search Highlight Feature Test1`` (api : ApiClient) = 
//        let query = new SearchQuery("country", "background = 'most prosperous countries'")
//        let highlight = new List<string>()
//        highlight.Add("background")
//        query.Highlights <- new HighlightOption(highlight |> Seq.toArray)
//        query.Highlights.FragmentsToReturn <- 2
//        query.Columns <- [| "country"; "background" |]
//        let result = api.Search(query).Result
//        result |> isSuccessful
//        (result |> data).Documents.Length >=? 0
//
