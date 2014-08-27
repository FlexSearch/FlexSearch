namespace FlexSearch.IntegrationTests

module ``Rest webservices tests - Indices`` = 
    open FlexSearch.Api
    open FlexSearch.Core
    open FlexSearch.Utility
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
    open FlexSearch.TestSupport
    open Autofac
    open Xunit
    open Xunit.Extensions
    open Microsoft.Owin.Testing
    open FlexSearch.TestSupport.RestHelpers
    
    type Dummy() = 
        do ()
    
    [<Theory; AutoMockIntegrationData>]
    let ``Accessing server root should return 200`` (server : TestServer) = 
        server
        |> request "GET" "/"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-1", "Creating an index without any data")>]
    let ``Creating an index without any parameters should return 200`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-2", "Duplicate index cannot be created")>]
    let ``Duplicate index cannot be created`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> ignore
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.BadRequest
        |> responseMatches "ErrorCode" "1002"
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-3", "")>]
    let ``Create index with two field 'firstname' & 'lastname'`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> withBody """
            {
                "Fields" : {
                    "firstname" : { FieldType : "Text" },
                    "lastname" : { FieldType : "Text" }
                }
            }
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-4", "")>]
    let ``Create an index with dynamic fields`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> withBody """
        {
                "Fields" : {
                    "firstname" : { FieldType : "Text" },
                    "lastname" : { FieldType : "Text" },
                    "fullname" : {FieldType : "Text", ScriptName : "fullnamescript"}
                },
                "Scripts" : {
                    fullnamescript : {
                        ScriptType : "ComputedField",
                        Source : "return fields.firstname + \" \" + fields.lastname;"
                    }
                }
        }        
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-5", "")>]
    let ``Create an index by setting all properties`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> withBody (JsonConvert.SerializeObject(IntegrationTestHelpers.MockIndexSettings(), jsonSettings))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
    
    [<Theory; AutoMockIntegrationData; Example("put-indices-id-1", "")>]
    let ``Update an index`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> withBody """
        {
                "Fields" : {
                    "firstname" : { FieldType : "Text" },
                    "lastname" : { FieldType : "Text" }
                },
        }        
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
        server
        |> request "PUT" ("/indices/" + indexName.ToString("N"))
        |> withBody """
        {
                "Fields" : {
                    "firstname" : { FieldType : "Text" },
                    "lastname" : { FieldType : "Text" },
                    "desc" : { FieldType : "Stored" }
                },
        }        
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
    
    [<Theory; AutoMockIntegrationData; Example("put-indices-id-2", "")>]
    let ``Trying to update an non existing index will return error`` (server : TestServer, indexName : Guid) = 
        server
        |> request "PUT" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.BadRequest
        |> responseMatches "ErrorCode" "1000"
    
    [<Theory; AutoMockIntegrationData; Example("delete-indices-id-1", "")>]
    let ``Delete an index by id`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
        server
        |> request "DELETE" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
    
    [<Theory; AutoMockIntegrationData; Example("delete-indices-id-2", "")>]
    let ``Trying to delete an non existing index will return error`` (server : TestServer, indexName : Guid) = 
        server
        |> request "DELETE" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.BadRequest
        |> responseMatches "ErrorCode" "1000"
    
    [<Theory; AutoMockIntegrationData; Example("get-indices-id-1", "")>]
    let ``Getting an index detail by name`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> withBody """
        {
                "Fields" : {
                    "firstname" : { FieldType : "Text" },
                    "lastname" : { FieldType : "Text" }
                },
        }        
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
        server
        |> request "GET" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "IndexName" (indexName.ToString("N"))
    
    [<Theory; AutoMockIntegrationData; Example("get-indices-id-2", "")>]
    let ``Getting an non existing index will return error`` (server : TestServer, indexName : Guid) = 
        server
        |> request "GET" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.BadRequest
        |> responseMatches "ErrorCode" "1000"
    
    [<Theory; AutoMockIntegrationData; Example("get-indices-id-status-1", "Get status of an index (offine)")>]
    let ``Newly created index is always offline`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
        server
        |> request "GET" ("/indices/" + indexName.ToString("N") + "/status")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "Status" "Offline"
    
    [<Theory; AutoMockIntegrationData; Example("put-indices-id-status-1", "")>]
    let ``Set status of an index 'online'`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
        server
        |> request "PUT" ("/indices/" + indexName.ToString("N") + "/status/online")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
    
    [<Theory; AutoMockIntegrationData; Example("put-indices-id-status-1", "")>]
    let ``Set status of an index 'offline'`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> withBody """
        {
            "Online" : "true"
        } 
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
        server
        |> request "PUT" ("/indices/" + indexName.ToString("N") + "/status/offline")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull

    [<Theory; AutoMockIntegrationData; Example("get-indices-id-exists-1", "")>]
    let ``Check if a given index exists`` (server : TestServer, indexName : Guid) = 
        server
        |> request "GET" ("/indices/contact/exists")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK

    [<Theory; AutoMockIntegrationData; Example("get-indices-1", "")>]
    let ``Get all indices`` (server : TestServer, indexName : Guid) = 
        server
        |> request "GET" ("/indices")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseShouldContain "contact"