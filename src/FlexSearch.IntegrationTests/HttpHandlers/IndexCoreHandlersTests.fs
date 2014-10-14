namespace FlexSearch.IntegrationTests.Rest

open Autofac
open FlexSearch.Api
open FlexSearch.Api.Messages
open FlexSearch.Client
open FlexSearch.Core
open FlexSearch.TestSupport
open FlexSearch.TestSupport.RestHelpers
open FlexSearch.Utility
open Microsoft.Owin.Testing
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
open Xunit
open Xunit.Extensions

module ``Index Creation Tests`` = 
    [<Theory; AutoMockIntegrationData>]
    let ``Accessing server root should return 200`` (server : TestServer) = 
        server
        |> request "GET" "/"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-1", "Creating an index without any data")>]
    let ``Creating an index without any parameters should return 200`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/")
        |> withBody (sprintf """{"IndexName" : "%s"}""" (indexName.ToString("N")))
        |> execute
        |> responseStatusEquals HttpStatusCode.Created
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-2", "Duplicate index cannot be created")>]
    let ``Duplicate index cannot be created`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        client.AddIndex(new Index(IndexName = indexName.ToString("N"))).Result |> ExpectSuccess
        let actual = client.AddIndex(new Index(IndexName = indexName.ToString("N"))).Result
        actual |> VerifyErrorCode Errors.INDEX_ALREADY_EXISTS
        handler |> VerifyHttpCode HttpStatusCode.Conflict
    
    [<Theory; AutoMockIntegrationData>]
    let ``Create response contains the id of the created index`` (client : IFlexClient, indexName : Guid, 
                                                                  handler : LoggingHandler) = 
        let actual = client.AddIndex(new Index(IndexName = indexName.ToString("N"))).Result
        actual |> ExpectSuccess
        handler |> VerifyHttpCode HttpStatusCode.Created
        Assert.Equal<string>((indexName.ToString("N")), actual.Data.Id)
    
    [<Theory; AutoMockIntegrationData>]
    let ``Index cannot be created without IndexName`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        let actual = client.AddIndex(new Index(IndexName = "")).Result
        handler |> VerifyHttpCode HttpStatusCode.BadRequest
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-3", "")>]
    let ``Create index with two field 'firstname' & 'lastname'`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/")
        |> withBody (sprintf """
        {
            "IndexName" : "%s",
            "Fields" : [
                {"FieldName" : "firstname" , FieldType : "Text" },
                {"FieldName" : "lastname" , FieldType : "Text" }
            ]
        }""" (indexName.ToString("N")))
        |> execute
        |> responseStatusEquals HttpStatusCode.Created
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-4", "")>]
    let ``Create an index with dynamic fields`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" "/indices/"
        |> withBody (sprintf """
        {
                "IndexName" : "%s",
                "Fields" : [
                   {"FieldName" : "firstname" , FieldType : "Text" },
                   {"FieldName" : "lastname" , FieldType : "Text" },
                   {"FieldName" : "fullname" , FieldType : "Text", ScriptName : "fullnamescript"}
                ],
                "Scripts" : [
                        {
                            "ScriptName" : "fullnamescript",
                            ScriptType : "ComputedField",
                            Source : "return fields.firstname + \" \" + fields.lastname;"
                        }
                    ]
        }        
        """ (indexName.ToString("N")))
        |> execute
        |> responseStatusEquals HttpStatusCode.Created
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-5", "")>]
    let ``Create an index by setting all properties`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        let index = MockIndexSettings()
        index.IndexName <- indexName.ToString("N")
        let actual = client.AddIndex(index).Result
        handler |> VerifyHttpCode HttpStatusCode.Created

module ``Index Update Tests`` = 
    [<Theory; AutoMockIntegrationData; Example("put-indices-id-1", "")>]
    let ``Update an index`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        let index = new Index(IndexName = indexName.ToString("N"))
        client.AddIndex(index).Result |> ExpectSuccess
        index.Fields.Add(new Field("firstname", FieldType.Text))
        index.Fields.Add(new Field("lastname", FieldType.Text))
        let actual = client.UpdateIndex(index).Result
        handler |> VerifyHttpCode HttpStatusCode.OK
    
    [<Theory; AutoMockIntegrationData; Example("put-indices-id-2", "")>]
    let ``Trying to update an non existing index will return error`` (client : IFlexClient, indexName : Guid, 
                                                                      handler : LoggingHandler) = 
        let actual = client.UpdateIndex(new Index(IndexName = indexName.ToString("N"))).Result
        actual |> VerifyErrorCode Errors.INDEX_NOT_FOUND
        handler |> VerifyHttpCode HttpStatusCode.NotFound

module ``Delete Index`` = 
    [<Theory; AutoMockIntegrationData; Example("delete-indices-id-1", "")>]
    let ``Delete an index by id`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        client.AddIndex(new Index(IndexName = indexName.ToString("N"))).Result |> ExpectSuccess
        let actual = client.DeleteIndex(indexName.ToString("N")).Result
        actual |> ExpectSuccess
        handler |> VerifyHttpCode HttpStatusCode.OK
    
    [<Theory; AutoMockIntegrationData; Example("delete-indices-id-2", "")>]
    let ``Trying to delete an non existing index will return error`` (client : IFlexClient, indexName : Guid, 
                                                                      handler : LoggingHandler) = 
        let actual = client.DeleteIndex(indexName.ToString("N")).Result
        actual |> VerifyErrorCode Errors.INDEX_NOT_FOUND
        handler |> VerifyHttpCode HttpStatusCode.NotFound

module ``Get Index Tests`` = 
    [<Theory; AutoMockIntegrationData; Example("get-indices-id-1", "")>]
    let ``Getting an index detail by name`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        let actual = client.GetIndex("contact").Result
        actual |> ExpectSuccess
        Assert.Equal<string>("contact", actual.Data.IndexName)
        handler |> VerifyHttpCode HttpStatusCode.OK
    
    [<Theory; AutoMockIntegrationData; Example("delete-indices-id-2", "")>]
    let ``Getting an non existing index will return error`` (client : IFlexClient, indexName : Guid, 
                                                             handler : LoggingHandler) = 
        let actual = client.DeleteIndex(indexName.ToString("N")).Result
        actual |> VerifyErrorCode Errors.INDEX_NOT_FOUND
        handler |> VerifyHttpCode HttpStatusCode.NotFound

module ``Index Other Services Tests`` = 
    [<Theory; AutoMockIntegrationData; Example("get-indices-id-status-1", "Get status of an index (offine)")>]
    let ``Newly created index is always offline`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        client.AddIndex(new Index(IndexName = indexName.ToString("N"))).Result |> ExpectSuccess
        let actual = client.GetIndexStatus(indexName.ToString("N")).Result
        actual |> ExpectSuccess
        Assert.Equal<IndexState>(IndexState.Offline, actual.Data.Status)
    
    [<Theory; AutoMockIntegrationData; Example("put-indices-id-status-1", "")>]
    let ``Set status of an index 'online'`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        client.AddIndex(new Index(IndexName = indexName.ToString("N"))).Result |> ExpectSuccess
        client.BringIndexOnline(indexName.ToString("N")).Result |> ExpectSuccess
        let actual = client.GetIndexStatus(indexName.ToString("N")).Result
        Assert.Equal<IndexState>(IndexState.Online, actual.Data.Status)
    
    [<Theory; AutoMockIntegrationData; Example("put-indices-id-status-1", "")>]
    let ``Set status of an index 'offline'`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        client.AddIndex(new Index(IndexName = indexName.ToString("N"), Online = true)).Result |> ExpectSuccess
        let actual = client.GetIndexStatus(indexName.ToString("N")).Result
        Assert.Equal<IndexState>(IndexState.Online, actual.Data.Status)
        client.SetIndexOffline(indexName.ToString("N")).Result |> ExpectSuccess
        let actual = client.GetIndexStatus(indexName.ToString("N")).Result
        Assert.Equal<IndexState>(IndexState.Offline, actual.Data.Status)
    
    [<Theory; AutoMockIntegrationData; Example("get-indices-id-exists-1", "")>]
    let ``Check if a given index exists`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        let actual = client.IndexExists("contact").Result
        actual |> ExpectSuccess
        Assert.Equal<bool>(true, actual.Data.Exists)
    
    [<Theory; AutoMockIntegrationData; Example("get-indices-1", "")>]
    let ``Get all indices`` (client : IFlexClient, handler : LoggingHandler) = 
        let actual = client.GetAllIndex().Result
        // Should have at least contact index
        Assert.True(actual.Data.Count >= 1)
