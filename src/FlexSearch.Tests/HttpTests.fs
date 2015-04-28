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
    
    let newIndex indexName = new Index.Dto(IndexName = indexName)
    
    let addField (index : Index.Dto) (fieldName : string) =
        index.Fields <- index.Fields |> Array.append [|new Field.Dto(fieldName)|]
    

    // ----------------------------------------------------------------------------
    // Test assertions for FlexClient based tests
    // ----------------------------------------------------------------------------
    let hasHttpStatusCode expected response = response |> snd =? expected

    let hasErrorCode expected (response : Response<_> * HttpStatusCode) = (response |> fst).Error.ErrorCode =? expected

    let isSuccessful response = response |> snd =? HttpStatusCode.OK

    let isCreated response = response |> snd =? HttpStatusCode.Created

    let responseStatusEquals status result = result.Response.StatusCode =? status

    let data (response : Response<_> * HttpStatusCode) = (response |> fst).Data


type ``Index Creation Tests``() = 

    member __.``Accessing server root should return 200`` () = 
        owinServer
        |> request "GET" "/"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
    
    [<Example("post-indices-id-1", "Creating an index without any data")>]
    member __.``Creating an index without any parameters should return 200`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.AddIndex(newIndex indexName).Result
        actual |> isCreated
        client.DeleteIndex(indexName).Result |> isSuccessful
    
    [<Example("post-indices-id-2", "Duplicate index cannot be created")>]
    member __.``Duplicate index cannot be created`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        let actual = client.AddIndex(index).Result
        actual |> hasErrorCode "INDEX_ALREADY_EXISTS"
        actual |> hasHttpStatusCode HttpStatusCode.Conflict
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    member __.``Create response contains the id of the created index`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
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
        index.Fields <- [| new Field.Dto("firstname"); new Field.Dto("lastname")|]
        client.AddIndex(index).Result |> isCreated
        client.DeleteIndex(indexName).Result |> isSuccessful
    
    [<Example("post-indices-id-4", "")>]
    member __.``Create an index with dynamic fields`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let index = newIndex indexName
        index.Fields <- [| new Field.Dto("firstname")
                           new Field.Dto("lastname")
                           new Field.Dto("fullname", ScriptName = "fullnamescript") |]
        index.Scripts <- 
            [| new Script.Dto(ScriptName = "fullnamescript", Source = "return fields.firstname + \" \" + fields.lastname;", ScriptType = ScriptType.Dto.ComputedField) |]
        client.AddIndex(index).Result |> isCreated
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("post-indices-id-5", "")>]
    member __.``Create an index by setting all properties`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        let actual = client.AddIndex(index).Result
        actual |> hasHttpStatusCode HttpStatusCode.Created
        client.DeleteIndex(index.IndexName).Result |> isSuccessful

type ``Index Update Tests``() = 
    [<Example("put-indices-id-1", "")>]
    member __.``Trying to update an index is not supported`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        let actual = client.UpdateIndex(index).Result
        actual |> hasErrorCode "HTTP_NOT_SUPPORTED"
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest

type ``Delete Index Tests``() = 
    [<Example("delete-indices-id-1", "")>]
    member __.``Delete an index by id`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("delete-indices-id-2", "")>]
    member __.``Trying to delete an non existing index will return error`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.DeleteIndex(indexName).Result
        actual |> hasErrorCode "INDEX_NOT_FOUND"
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest

type ``Get Index Tests``() = 
    [<Example("get-indices-id-1", "")>]
    member __.``Getting an index detail by name`` (client : FlexClient, handler : LoggingHandler) = 
        let actual = client.GetIndex("contact").Result
        actual |> isSuccessful
        (actual |> data).IndexName =? "contact"
        actual |> hasHttpStatusCode HttpStatusCode.OK
    
    [<Example("get-indices-id-2", "")>]
    member __.``Getting an non existing index will return error`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.GetIndex(indexName).Result
        actual |> hasErrorCode "INDEX_NOT_FOUND"
        actual |> hasHttpStatusCode HttpStatusCode.NotFound

type ``Index Other Services Tests``() = 
    [<Example("get-indices-id-status-1", "Get status of an index (offine)")>]
    member __.``Newly created index is always offline`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        index.Online <- false
        client.AddIndex(index).Result |> isCreated
        let actual = client.GetIndexStatus(index.IndexName).Result
        actual |> isSuccessful
        (actual |> data).Status =? IndexState.Offline
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("put-indices-id-status-1", "")>]
    member __.``Set status of an index 'online'`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        index.Online <- false
        client.AddIndex(index).Result |> isCreated
        client.BringIndexOnline(index.IndexName).Result |> isSuccessful
        let actual = client.GetIndexStatus(index.IndexName).Result
        (actual |> data).Status =? IndexState.Online
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("put-indices-id-status-1", "")>]
    member __.``Set status of an index 'offline'`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        let actual = client.GetIndexStatus(index.IndexName).Result
        actual |> isSuccessful
        (actual |> data).Status =? IndexState.Online
        client.SetIndexOffline(index.IndexName).Result |> isSuccessful
        let actual = client.GetIndexStatus(index.IndexName).Result
        (actual |> data).Status =? IndexState.Offline
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