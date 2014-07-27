namespace FlexSearch.IntegrationTests

module ``Rest webservices tests - Search`` = 
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
    
    let Query (body : string) (recordsReturned : int) (available : int) (server : TestServer) = 
        let query = sprintf """{"QueryString": "%s"}""" body
        server
        |> request "POST" "/indices/contact/search?c=firstname,lastname"
        |> withBody query
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "RecordsReturned" (recordsReturned.ToString())
        |> responseMatches "TotalAvailable" (available.ToString())
    
    [<Theory; AutoMockIntegrationData>]
    let ``Term Query Test 1`` (server : TestServer) = server |> Query "firstname = 'Kathy' and lastname = 'Banks'" 1 1
    
    [<Theory; AutoMockIntegrationData>]
    let ``Term Query Test 2`` (server : TestServer) = server |> Query "firstname eq 'Kathy' and lastname eq 'Banks'" 1 1
    
    [<Theory; AutoMockIntegrationData>]
    let ``Fuzzy Query Test 1`` (server : TestServer) = server |> Query "firstname fuzzy 'Kathy'" 3 3
    
    [<Theory; AutoMockIntegrationData>]
    let ``Fuzzy Query Test 2`` (server : TestServer) = server |> Query "firstname ~= 'Kathy'" 3 3
    
    [<Theory; AutoMockIntegrationData>]
    let ``Fuzzy Query Test 3`` (server : TestServer) = server |> Query "firstname ~= 'Kathy' {slop : '2'}" 3 3
    
    [<Theory; AutoMockIntegrationData>]
    let ``Phrase Query Test 1`` (server : TestServer) = server |> Query "description match 'Nunc purus'" 4 4
    
    [<Theory; AutoMockIntegrationData>]
    let ``Wildcard Query Test 1`` (server : TestServer) = server |> Query "firstname like 'ca*'" 3 3
    
    [<Theory; AutoMockIntegrationData>]
    let ``Wildcard Query Test 2`` (server : TestServer) = server |> Query "firstname %= 'ca*'" 3 3
    
    [<Theory; AutoMockIntegrationData>]
    let ``Wildcard Query Test 3`` (server : TestServer) = server |> Query "firstname %= 'Cat?y'" 1 1
    
    [<Theory; AutoMockIntegrationData>]
    let ``Regex Query Test 1`` (server : TestServer) = server |> Query "firstname regex '[ck]Athy'" 3 3
    
    [<Theory; AutoMockIntegrationData>]
    let ``Matchall Query Test 1`` (server : TestServer) = server |> Query "firstname matchall '*'" 10 50
    
    [<Theory; AutoMockIntegrationData>]
    let ``NumericRange Query Test 1`` (server : TestServer) = server |> Query "cvv2 > '100'" 10 48
    
    [<Theory; AutoMockIntegrationData>]
    let ``NumericRange Query Test 2`` (server : TestServer) = server |> Query "cvv2 > '200'" 10 41
    
    [<Theory; AutoMockIntegrationData>]
    let ``NumericRange Query Test 3`` (server : TestServer) = server |> Query "cvv2 < '150'" 7 7
    
    [<Theory; AutoMockIntegrationData>]
    let ``NumericRange Query Test 4`` (server : TestServer) = server |> Query "cvv2 <= '500'" 10 26
    
    [<Theory; AutoMockIntegrationData>]
    let ``Search Highlight Feature Test1`` (server : TestServer) = 
        let query = new SearchQuery("contact", " description = 'Nullam'")
        let highlight = new List<string>()
        highlight.Add("description")
        query.Highlights <- new HighlightOption(highlight)
        server
        |> request "POST" "/indices/contact/search?c=firstname,lastname,description"
        |> withBody """
        {
          "Count": 2,  
          "Highlights": {
            "FragmentsToReturn": 2,
            "HighlightedFields": [
              "description"
            ],
            "PostTag": "</B>",
            "PreTag": "</B>"
          },
          "QueryString": " description = 'Nullam'",
          }   
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> ignore
