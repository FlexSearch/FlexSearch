namespace FlexSearch.IntegrationTests

module ``Rest webservices tests - Documents`` = 
    open FlexSearch.Api
    open FlexSearch.Api.Message
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


    [<Theory; AutoMockIntegrationData>]
    let ``Document test 1`` (server : TestServer, indexName: Guid) = 
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
        |> ignore
        Thread.Sleep(5000)
        server
        |> request "GET" ("/indices/" + indexName.ToString("N") + "/documents/51")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches Constants.IdField "51"
        |> ignore
        server
        |> request "DELETE" ("/indices/" + indexName.ToString("N") + "/documents/51")
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> ignore