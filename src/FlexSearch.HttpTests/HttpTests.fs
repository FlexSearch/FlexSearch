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
open System.Linq
open System.Reflection

// An alias so that we avoid opening FlexSearch.Core
type Country = FlexSearch.Core.Country

type ``All Tests``(serverApi : ServerApi, indicesApi : IndicesApi) = 
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

    do  mockIndexSettings() |> ignore
        serverApi.SetupDemo() |> isSuccessful

    member __.``Calling ping should return success``((api : ServerApi, handler : LoggingHandler)) =
        api.Ping()
        |> fun r -> r |> isSuccessful; r
        |> fun r -> r.Data =? true

    [<Example("post-indices-id-1", "Creating an index without any data")>]
    member __.``Creating an index without any parameters should return 200`` ((api : IndicesApi, handler : LoggingHandler), indexName : string) = 
        api.CreateIndexWithHttpInfo(newIndex indexName) |> isCreated
        handler |> log "post-indices-id-1" 0
        api.DeleteIndex(indexName) |> isSuccessful
    
    [<Example("post-indices-id-2", "Duplicate index cannot be created")>]
    member __.``Duplicate index cannot be created`` ((api : IndicesApi, handler : LoggingHandler), index : Index) = 
        api.CreateIndex(index) |> isSuccessful
        api.CreateIndexWithHttpInfo(index)
        |> fun r -> r.Data.Data =? false; r
        |> hasStatusCode HttpStatusCode.Conflict
        |> hasApiErrorCode "IndexAlreadyExists"
        |> ignore
        handler |> log "post-indices-id-2" 1
        api.DeleteIndex(index.IndexName) |> isSuccessful
        
    member __.``Create index response returns true`` ((api : IndicesApi, handler : LoggingHandler), index : Index) = 
        let actual = api.CreateIndexWithHttpInfo(index)
        actual |> isCreated
        actual.Data.Data =? true
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    member __.``Index cannot be created without IndexName`` ((api : IndicesApi, handler : LoggingHandler)) = 
        api.CreateIndexWithHttpInfo(newIndex "") 
        |> hasStatusCode HttpStatusCode.BadRequest
        |> ignore
    
    [<Example("post-indices-id-3", "")>]
    member __.``Create index with two field 'firstname' & 'lastname'`` ((api : IndicesApi, handler : LoggingHandler), indexName : string) =
        let index = newIndex indexName
        index.Fields <- [| new Field("firstname")
                           new Field("lastname") |]
        api.CreateIndex(index) |> isSuccessful
        handler |> log "post-indices-id-3" 0
        api.DeleteIndex(indexName) |> isSuccessful
    
    //    [<Example("post-indices-id-4", "")>]
    //    member __.``Create an index with dynamic fields`` (api : IndicesApi, indexName : string) = 
    //        let index = newIndex indexName
    //        index.Fields <- [| new Field.Dto("firstname")
    //                           new Field.Dto("lastname")
    //                           new Field.Dto("fullname", ScriptName = "fullnamescript") |]
    //        index.Scripts <- 
    //            [| new Script.Dto(ScriptName = "fullnamescript", Source = "return fields.firstname + \" \" + fields.lastname;", ScriptType = ScriptType.Dto.ComputedField) |]
    //        api.CreateIndex(index).Result |> isCreated
    //        api.DeleteIndex(index.IndexName).Result |> isSuccessful
    [<Example("post-indices-id-5", "")>]
    member __.``Create an index by setting all properties`` ((api : IndicesApi, handler : LoggingHandler), index : Index) =
        api.CreateIndexWithHttpInfo(index)
        |> hasStatusCode HttpStatusCode.Created
        |> ignore
        handler |> log "post-indices-id-5" 0
        api.DeleteIndex(index.IndexName) |> isSuccessful

//type ``Index Update Tests``() = 
    
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
    member __.``Trying to update index fields should return success`` ((api : IndicesApi, handler : LoggingHandler), index : Index) =
        api.CreateIndexWithHttpInfo(index) |> isCreated
        index.Fields.[0].FieldName <- "modified"
        let fields = new FieldsUpdateRequest(Fields = index.Fields)
        api.UpdateIndexFields(fields, index.IndexName) 
        |> fun r -> r |> isSuccessful; r
        |> fun r -> r.Data =? true
        handler |> log "put-indices-id-2" 1
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    [<Example("put-indices-id-3", "")>]
    member __.``Trying to update index search profile should return success`` ((api : IndicesApi, handler : LoggingHandler), index : Index) =
        api.CreateIndexWithHttpInfo(index) |> isCreated
        let sp = new SearchQuery(index.IndexName, "matchall(et1, 'x')", QueryName = "all")
        api.UpdateIndexPredefinedQuery(sp, index.IndexName) |> isSuccessful
        handler |> log "put-indices-id-3" 1
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    [<Example("put-indices-id-4", "")>]
    member __.``Trying to update index configuration should return success`` ((api : IndicesApi, handler : LoggingHandler),  index : Index) = 
        api.CreateIndexWithHttpInfo(index) |> isCreated
        let conf = new IndexConfiguration(CommitTimeSeconds = 100)
        api.UpdateIndexConfiguration(conf, index.IndexName) |> isSuccessful
        handler |> log "put-indices-id-4" 1
        api.DeleteIndex(index.IndexName) |> isSuccessful

//type ``Delete Index Tests``() = 
    [<Example("delete-indices-id-1", "")>]
    member __.``Delete an index by id`` ((api : IndicesApi, handler : LoggingHandler), index : Index) = 
        api.CreateIndexWithHttpInfo(index) |> isCreated
        api.DeleteIndex(index.IndexName) |> isSuccessful
        handler |> log "delete-indices-id-1" 1

    [<Example("delete-indices-id-2", "")>]
    member __.``Trying to delete an non existing index will return error`` ((api : IndicesApi, handler : LoggingHandler), indexName : string) = 
        api.DeleteIndexWithHttpInfo(indexName)
        |> fun r -> r.Data.Data =? false; r
        |> hasApiErrorCode "IndexNotFound"
        |> hasStatusCode HttpStatusCode.BadRequest
        |> ignore
        handler |> log "delete-indices-id-2" 0

//type ``Get Index Tests``() = 
    [<Example("get-indices-id-1", "")>]
    member __.``Getting an index detail by name`` ((api : IndicesApi, handler : LoggingHandler)) = 
        let actual = api.GetIndexWithHttpInfo("contact")
        actual.Data |> isSuccessful
        actual.Data.Data.IndexName =? "contact"
        actual |> hasStatusCode HttpStatusCode.OK |> ignore
        handler |> log "get-indices-id-1" 0

//type ``Get Non existing Index Tests``() = 
    [<Example("get-indices-id-2", "")>]
    member __.``Getting an non existing index will return error`` ((api : IndicesApi, handler : LoggingHandler), indexName : string) = 
        api.GetIndexWithHttpInfo(indexName)
        |> hasApiErrorCode "IndexNotFound"
        |> hasStatusCode HttpStatusCode.NotFound
        |> ignore
        handler |> log "get-indices-id-2" 0

//type ``Index Other Services Tests``() = 
    
    [<Example("get-indices-id-status-1", "Get status of an index (offine)")>]
    member __.``Newly created index is always offline`` ((api : IndicesApi, handler : LoggingHandler), index : Index) = 
        index.Active <- false
        api.CreateIndexWithHttpInfo(index) |> isCreated
        let actual = api.GetIndexStatus(index.IndexName)
        actual |> isSuccessful
        actual.Data.IndexStatus =? IndexStatus.Offline
        handler |> log "get-indices-id-status-1" 1
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    [<Example("put-indices-id-status-1", "")>]
    member __.``Set status of an index 'online'`` ((api : IndicesApi, handler : LoggingHandler), index : Index) = 
        index.Active <- false
        api.CreateIndex(index) |> isSuccessful
        api.UpdateIndexStatus(index.IndexName, "online") |> isSuccessful
        api.GetIndexStatus(index.IndexName).Data.IndexStatus =? IndexStatus.Online
        handler |> log "put-indices-id-status-1" 1
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    member __.``Set status of an index 'offline'`` (api : IndicesApi, index : Index) = 
        api.CreateIndex(index) |> isSuccessful
        api.GetIndexStatus(index.IndexName)
        |> fun r -> r.Data.IndexStatus =? IndexStatus.Online; r
        |> isSuccessful

        api.UpdateIndexStatus(index.IndexName, "offline") |> isSuccessful

        api.GetIndexStatus(index.IndexName).Data.IndexStatus =? IndexStatus.Offline
        api.DeleteIndex(index.IndexName) |> isSuccessful
    
    [<Example("get-indices-id-exists-1", "")>]
    member __.``Check if a given index exists`` ((api : IndicesApi, handler : LoggingHandler)) = 
        api.IndexExists("contact")
        |> fun r -> r.Data.Exists =? true; r
        |> isSuccessful
        handler |> log "get-indices-id-exists-1" 0

    member __.``Checking if a given index exists should not return error if index not present`` ((api : IndicesApi, handler : LoggingHandler)) = 
        let actual = api.IndexExists("index-which-does-not-exist")
        actual |> isSuccessful
        actual.Data.Exists =? false
        handler |> log "get-indices-id-exists-1" 0

    
    [<Example("get-indices-1", "")>]
    member __.``Get all indices`` ((api : IndicesApi, handler : LoggingHandler)) = 
        api.GetAllIndices()
        // Should have at least contact index
        |> fun r -> r.Data.Length >=? 1
        handler |> log "get-indices-1" 0

    [<Example("error-details", "Field validation error message should mention missing field")>]
    member __.``Field validation error message should mention missing field`` ((api : IndicesApi, handler : LoggingHandler), index : Index) = 
        index.IndexName <- "a"   // Shouldn't be able to create an index with a name with less than 2 characters
        api.CreateIndexWithHttpInfo(index)
        |> fun r -> r.Data.Error <>? null
                    r.Data.Error.Message.ToLower().Contains("indexname") =? true
                    r.Data.Error.Properties |> Seq.exists (fun kv -> kv.Key.ToLower() = "fieldname"
                                                                     && kv.Value.ToLower() = "indexname")
                                            =? true

//type ``Document Tests``(demoApi : ServerApi) = 

    [<Example("get-indices-id-documents-1", "")>]
    member __.``Get top 10 documents from an index`` ((api : DocumentsApi, handler : LoggingHandler), indexName : string) = 
        let actual = api.GetDocuments("country")
        actual |> isSuccessful
        handler |> log "get-indices-id-documents-1" 0
        actual.Data.RecordsReturned =? 10
    
    [<Example("post-indices-id-documents-id-2", "")>]
    member __.``Add a document to an index`` ((api : CommonApi, handler : LoggingHandler), indexName : string) = 
        let actual = createDocument api indexName
        actual |> fst |> isSuccessful
        handler |> log "post-indices-id-documents-id-2" 1
        (actual |> fst).Data.Id =? "1"
        api.DeleteIndex(indexName) |> isSuccessful
    
    member __.``Cannot add a document without an id`` ((api : CommonApi, handler : LoggingHandler), indexName : string) = 
        api.CreateIndexWithHttpInfo(testIndex indexName) |> isCreated
        let document = new Document(indexName = indexName, id = "")
        api.CreateDocumentWithHttpInfo(document, indexName) 
        |> hasStatusCode HttpStatusCode.BadRequest
        |> ignore
        api.DeleteIndex(indexName) |> isSuccessful
    
    [<Example("put-indices-id-documents-id-1", "")>]
    member __.``Update a document to an index`` ((api : CommonApi, handler : LoggingHandler), indexName : string) = 
        // Create the document
        let (result, document) = createDocument api indexName
        result |> isSuccessful
        // Update the document
        document.Fields.["lastname"] <- "Rajvanshi1"
        let actual = api.CreateOrUpdateDocument(document, indexName, document.Id)
        handler |> log "put-indices-id-documents-id-2" 1
        actual |> isSuccessful
        api.DeleteIndex(indexName) |> isSuccessful
    
    [<Example("get-indices-id-documents-id-1", "")>]
    member __.``Get a document from an index`` ((api : DocumentsApi, handler : LoggingHandler)) = 
        let actual = api.GetDocument("country", "1")
        actual |> isSuccessful
        actual.Data.Id =? "1"
        actual.Data.Fields.["countryname"] =? "Afghanistan"
        handler |> log "get-indices-id-documents-id-1" 0
    
    [<Example("get-indices-id-documents-id-2", "")>]
    member __.``Non existing document should return Not found`` ((api : CommonApi, handler : LoggingHandler), indexName : string) = 
        createDocument api indexName
        |> fst |> isSuccessful
        api.GetDocumentWithHttpInfo(indexName, "2")
        |> hasStatusCode HttpStatusCode.NotFound
        |> ignore
        handler |> log "get-indices-id-documents-id-2" 1
        api.DeleteIndex(indexName) |> isSuccessful

    member __.``CreateDocument request should use indexName from request path url`` (docApi : DocumentsApi, indexApi : IndicesApi : IndicesApi, indexName : string) =
        indicesApi.CreateIndexWithHttpInfo(testIndex indexName) |> isCreated
        let document = new Document(indexName = null, id = "1")
        document.Fields.Add("firstname", "Seemant")
        document.Fields.Add("lastname", "Rajvanshi")

        docApi.CreateDocument(document, indexName) |> isSuccessful

//type ``Demo index Test``() = 
    member __.``Setting up the demo index creates the country index`` (serverApi : ServerApi, (indicesApi : IndicesApi, handler : LoggingHandler)) = 
        serverApi.SetupDemo() |> isSuccessful
        indicesApi.GetIndex("country") |> isSuccessful
    
    [<Example("post-indices-search-highlighting-1", "Text highlighting example")>]
    member __.``Search Highlight Feature Test1`` ((api : SearchApi, handler : LoggingHandler)) = 
        let query = new SearchQuery("country", "anyOf(background, 'most prosperous countries')")
        let highlight = [| "background" |]
        query.Highlights <- new HighlightOption(highlight)
        query.Highlights.FragmentsToReturn <- 2
        query.Columns <- [| "country"; "background" |]
        api.Search(query.IndexName, query)
        |> fun r -> r |> isSuccessful; r
        |> fun r -> r.Data.Documents.Length >=? 0
        handler |> log "post-indices-search-highlighting-1" 0

    member __.``Analyzing some text should return success`` (analyzerApi : AnalyzerApi) = 
        analyzerApi.AnalyzeText(new AnalyzeText("text to analyze", "standard"), "standard") 
        |> fun r -> r |> isSuccessful; r
        |> fun r -> r.Data.Length =? 2
        
    member __.``Should handle SearchQuery DTO errors gracefully`` (api : SearchApi) =
        let query = new SearchQuery("country", null)
        api.GetSearchWithHttpInfo("country", query)
        |> fun r -> r.Data.Error <>? null
                    r.StatusCode <>? int HttpStatusCode.InternalServerError
                    r.Data.Error.OperationCode.ToLower() =? "fieldvalidationfailed"

    member __.``Deleting documents by search should delete all documents returned by query``
        ((documentsApi: DocumentsApi, handler: LoggingHandler),
         commonApi: CommonApi,
         indexApi: IndicesApi,
         indexName : string) =
        createDocument commonApi indexName |> fst |> isSuccessful
        documentsApi.DeleteDocumentsBySearch(indexName, "anyof(firstname, 'Seemant')") |> isSuccessful
        handler |> log "delete-documents-by-search" 0
        indexApi.RefreshIndex indexName |> isSuccessful
        documentsApi.GetDocuments indexName
        |> fun r -> r |> isSuccessful; r
        |> fun r -> r.Data.RecordsReturned =? 0
        commonApi.DeleteIndex indexName

    interface IDisposable with
        member __.Dispose() = 
            indicesApi.DeleteIndex("country") |> isSuccessful
            indicesApi.DeleteIndex("contact") |> isSuccessful
            