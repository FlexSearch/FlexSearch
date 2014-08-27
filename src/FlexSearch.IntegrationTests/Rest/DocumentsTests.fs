namespace FlexSearch.IntegrationTests

module ``Rest webservices tests - Documents`` = 
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
    
    [<Theory; AutoMockIntegrationData; Example("get-indices-id-documents-1", "")>]
    let ``Get top 10 documents from an index`` (server : TestServer) = 
        server
        |> request "GET" ("/indices/contact/documents")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        //|> responseContainsHeader "RecordsReturned" "10"
        //|> responseContainsHeader "TotalAvailable" "50"
        |> ignore
    
    let DocumentTestBuilder(server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> withBody """
        {
            "Online": true,
            "Fields" : {
                "firstname" : { FieldType : "Text" },
                "lastname" : { FieldType : "Text" }
            }
        }
"""
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> ignore
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-documents-id-1", "")>]
    let ``Add a document to an index`` (server : TestServer, indexName : Guid) = 
        DocumentTestBuilder(server, indexName)
        server
        |> request "POST" ("/indices/" + indexName.ToString("N") + "/documents/51")
        |> withBody """
            {
                "firstname" : "Seemant",
                "lastname" : "Rajvanshi"
            }
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
    
    [<Theory; AutoMockIntegrationData; Example("put-indices-id-documents-id-1", "")>]
    let ``Update a document in an index`` (server : TestServer, indexName : Guid) = 
        DocumentTestBuilder(server, indexName)
        server
        |> request "PUT" ("/indices/" + indexName.ToString("N") + "/documents/51")
        |> withBody """
            {
                "firstname" : "Seemant",
                "lastname" : "Rajvanshi"
            }
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
    
    [<Theory; AutoMockIntegrationData; Example("get-indices-id-documents-id-1", "")>]
    let ``Get a document from an index`` (server : TestServer, indexName : Guid) = 
        ``Add a document to an index`` (server, indexName) |> ignore
        Thread.Sleep(5000)
        server
        |> request "GET" ("/indices/" + indexName.ToString("N") + "/documents/51")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches Constants.IdField "51"
    
    [<Theory; AutoMockIntegrationData; Example("delete-indices-id-documents-id-1", "")>]
    let ``Delete a document from an index`` (server : TestServer, indexName : Guid) = 
        ``Add a document to an index`` (server, indexName) |> ignore
        Thread.Sleep(5000)
        server
        |> request "DELETE" ("/indices/" + indexName.ToString("N") + "/documents/51")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
