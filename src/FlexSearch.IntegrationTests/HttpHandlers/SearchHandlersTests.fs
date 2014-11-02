namespace FlexSearch.IntegrationTests.Rest

module ``Search Tests`` = 
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
    open FlexSearch.Client

    let Query (queryString : string) (recordsReturned : int) (available : int) (client : IFlexClient) = 
        let searchQuery = new SearchQuery("contact", queryString)
        searchQuery.Columns.Add("firstname")
        searchQuery.Columns.Add("lastname")
        let response = client.Search(searchQuery).Result
        Assert.Equal<int>(recordsReturned, response.Data.RecordsReturned)
        Assert.Equal<int>(available, response.Data.TotalAvailable)
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-term-1", "Term search using '=' operator")>]
    let ``Term Query Test 1`` (client : IFlexClient) = client |> Query "firstname = 'Kathy' and lastname = 'Banks'" 1 1 
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-term-2", "Term search using 'eq' operator")>]
    let ``Term Query Test 2`` (client : IFlexClient) = client |> Query "firstname eq 'Kathy' and lastname eq 'Banks'" 1 1
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-fuzzy-1", "Fuzzy search using 'fuzzy' operator")>]
    let ``Fuzzy Query Test 1`` (client : IFlexClient) = client |> Query "firstname fuzzy 'Kathy'" 3 3
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-fuzzy-2", "Fuzzy search using '~=' operator")>]
    let ``Fuzzy Query Test 2`` (client : IFlexClient) = client |> Query "firstname ~= 'Kathy'" 3 3
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-fuzzy-3", "Fuzzy search using slop parameter")>]
    let ``Fuzzy Query Test 3`` (client : IFlexClient) = client |> Query "firstname ~= 'Kathy' {slop : '2'}" 3 3
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-phrase-1", "Phrase search using match operator")>]
    let ``Phrase Query Test 1`` (client : IFlexClient) = client |> Query "description match 'Nunc purus'" 4 4
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-wildcard-1", "Wildcard search using 'like' operator")>]
    let ``Wildcard Query Test 1`` (client : IFlexClient) = client |> Query "firstname like 'ca*'" 3 3
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-wildcard-2", "Wildcard search using '%=' operator")>]
    let ``Wildcard Query Test 2`` (client : IFlexClient) = client |> Query "firstname %= 'ca*'" 3 3
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-wildcard-3", "Wildcard search with single character operator")>]
    let ``Wildcard Query Test 3`` (client : IFlexClient) = client |> Query "firstname %= 'Cat?y'" 1 1
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-regex-1", "Regex search using regex operator")>]
    let ``Regex Query Test 1`` (client : IFlexClient) = client |> Query "firstname regex '[ck]Athy'" 3 3
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-matchall-1", "Match all search using 'matchall' operator")>]
    let ``Matchall Query Test 1`` (client : IFlexClient) = client |> Query "firstname matchall '*'" 10 50
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-range-1", "Greater than '>' operator")>]
    let ``NumericRange Query Test 1`` (client : IFlexClient) = client |> Query "cvv2 > '100'" 10 48
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-range-2", "Greater than or equal to '>=' operator")>]
    let ``NumericRange Query Test 2`` (client : IFlexClient) = client |> Query "cvv2 >= '200'" 10 41
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-range-3", "Smaller than '<' operator")>]
    let ``NumericRange Query Test 3`` (client : IFlexClient) = client |> Query "cvv2 < '150'" 7 7
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-range-1", "Smaller than or equal to '<=' operator")>]
    let ``NumericRange Query Test 4`` (client : IFlexClient) = client |> Query "cvv2 <= '500'" 10 26
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-search-highlighting-1", "Text highlighting example")>]
    let ``Search Highlight Feature Test1`` (client : IFlexClient) = 
        let query = new SearchQuery("contact", "description = 'Nullam'")
        let highlight = new List<string>()
        highlight.Add("description")
        query.Highlights <- new HighlightOption(highlight)
        query.Highlights.FragmentsToReturn <- 2
        query.Columns.Add("firstname")
        query.Columns.Add("lastname")
        query.Columns.Add("description")

        let result = client.Search(query).Result
        Assert.True(result.Data.Documents.Count > 0)
