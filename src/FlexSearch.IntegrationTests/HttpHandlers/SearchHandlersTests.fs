namespace FlexSearch.IntegrationTests.Rest

module ``Search Tests`` = 
    open Autofac
    open FlexSearch.Api
    open FlexSearch.Client
    open FlexSearch.TestSupport
    open FlexSearch.TestSupport.RestHelpers
    open System.Collections.Generic
    open System.Linq
    open Xunit
    open Xunit.Extensions
    
    let indexData = Container.Resolve<FlexSearch.Core.Services.DemoIndexService>().DemoData().Value
    
    let Query (queryString : string) (recordsReturned : int) (available : int) (client : IFlexClient) = 
        let searchQuery = new SearchQuery("country", queryString)
        searchQuery.Count <- 300
        searchQuery.Columns.Add("countryname")
        searchQuery.Columns.Add("agriproducts")
        searchQuery.Columns.Add("governmenttype")
        searchQuery.Columns.Add("population")
        let response = client.Search(searchQuery).Result
        response |> ExpectSuccess
        response.Data.Documents 
        |> Seq.iter 
               (fun x -> 
               printfn "Country Name:%s Agri products:%s Government type:%s" x.Fields.["countryname"] 
                   x.Fields.["agriproducts"] x.Fields.["governmenttype"])
        Assert.Equal<int>(recordsReturned, response.Data.RecordsReturned)
    
    [<Theory; RestData; Example("post-indices-search-term-1", "Term search using '=' operator")>]
    let ``Term Query Test 1`` (client : IFlexClient) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
        client |> Query "agriproducts = 'rice' and agriproducts = 'wheat'" expected 1
    
    [<Theory; RestData; Example("post-indices-search-term-2", "Term search using multiple words")>]
    let ``Term Query Test 2`` (client : IFlexClient) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") && x.AgriProducts.Contains("wheat")).Count()
        client |> Query "agriproducts = 'rice wheat'" expected 1
    
    [<Theory; RestData; Example("post-indices-search-term-3", "Term search using '=' operator")>]
    let ``Term Query Test 3`` (client : IFlexClient) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
        client |> Query "agriproducts eq 'rice' or agriproducts eq 'wheat'" expected 1
    
    [<Theory; RestData; Example("post-indices-search-term-4", "Term search using '=' operator")>]
    let ``Term Query Test 4`` (client : IFlexClient) = 
        let expected = 
            indexData.Where(fun x -> x.AgriProducts.Contains("rice") || x.AgriProducts.Contains("wheat")).Count()
        client |> Query "agriproducts eq 'rice wheat' {clausetype : 'or'}" expected 1
    
    [<Theory; RestData; Example("post-indices-search-fuzzy-1", "Fuzzy search using 'fuzzy' operator")>]
    let ``Fuzzy Query Test 1`` (client : IFlexClient) = client |> Query "countryname fuzzy 'Iran'" 2 3
    
    [<Theory; RestData; Example("post-indices-search-fuzzy-2", "Fuzzy search using '~=' operator")>]
    let ``Fuzzy Query Test 2`` (client : IFlexClient) = client |> Query "countryname ~= 'Iran'" 2 3
    
    [<Theory; RestData; Example("post-indices-search-fuzzy-3", "Fuzzy search using slop parameter")>]
    let ``Fuzzy Query Test 3`` (client : IFlexClient) = client |> Query "countryname ~= 'China' {slop : '2'}" 3 3
    
    [<Theory; RestData; Example("post-indices-search-phrase-1", "Phrase search using match operator")>]
    let ``Phrase Query Test 1`` (client : IFlexClient) = 
        let expected = indexData.Where(fun x -> x.GovernmentType.Contains("federal parliamentary democracy")).Count()
        client |> Query "governmenttype match 'federal parliamentary democracy'" expected 4
    
    [<Theory; RestData; Example("post-indices-search-phrase-2", "Phrase search with slop of 4")>]
    let ``Phrase Query Test 2`` (client : IFlexClient) = 
        client |> Query "governmenttype match 'parliamentary monarchy' {slop : '4'}" 6 4
    
    [<Theory; RestData; Example("post-indices-search-phrase-3", "Phrase search with slop of 4")>]
    let ``Phrase Query Test 3`` (client : IFlexClient) = 
        client |> Query "governmenttype match 'monarchy parliamentary' {slop : '4'}" 3 4
    
    [<Theory; RestData; Example("post-indices-search-wildcard-1", "Wildcard search using 'like' operator")>]
    let ``Wildcard Query Test 1`` (client : IFlexClient) = 
        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni"))
        client |> Query "countryname like '*uni*'" (expected.Count()) 3
    
    [<Theory; RestData; Example("post-indices-search-wildcard-2", "Wildcard search using '%=' operator")>]
    let ``Wildcard Query Test 2`` (client : IFlexClient) = 
        let expected = indexData.Where(fun x -> x.CountryName.ToLowerInvariant().Contains("uni")).Count()
        client |> Query "countryname %= '*uni*'" expected 3
    
    [<Theory; RestData; Example("post-indices-search-wildcard-3", "Wildcard search with single character operator")>]
    let ``Wildcard Query Test 3`` (client : IFlexClient) = 
        let expected = 
            indexData.Where(fun x -> 
                     System.Text.RegularExpressions.Regex.Match(x.CountryName.ToLowerInvariant(), "unit[a-z]?d").Success)
                     .Count()
        client |> Query "countryname %= 'Unit?d'" expected 1
    
    [<Theory; RestData; Example("post-indices-search-regex-1", "Regex search using regex operator")>]
    let ``Regex Query Test 1`` (client : IFlexClient) = 
        let expected = 
            indexData.Where(fun x -> 
                     System.Text.RegularExpressions.Regex.Match(x.AgriProducts.ToLowerInvariant(), "[ms]ilk").Success)
                     .Count()
        client |> Query "agriproducts regex '[ms]ilk'" expected 3
    
    [<Theory; RestData; Example("post-indices-search-matchall-1", "Match all search using 'matchall' operator")>]
    let ``Matchall Query Test 1`` (client : IFlexClient) = 
        let expected = indexData.Count
        client |> Query "countryname matchall '*'" expected 50
    
    [<Theory; RestData; Example("post-indices-search-range-1", "Greater than '>' operator")>]
    let ``NumericRange Query Test 1`` (client : IFlexClient) = 
        let expected = indexData.Where(fun x -> x.Population > 1000000L).Count()
        client |> Query "population > '1000000'" expected 48
    
    [<Theory; RestData; Example("post-indices-search-range-2", "Greater than or equal to '>=' operator")>]
    let ``NumericRange Query Test 2`` (client : IFlexClient) = 
        let expected = indexData.Where(fun x -> x.Population >= 1000000L).Count()
        client |> Query "population >= '1000000'" expected 48
    
    [<Theory; RestData; Example("post-indices-search-range-3", "Smaller than '<' operator")>]
    let ``NumericRange Query Test 3`` (client : IFlexClient) = 
        let expected = indexData.Where(fun x -> x.Population < 1000000L).Count()
        client |> Query "population < '1000000'" expected 48
    
    [<Theory; RestData; Example("post-indices-search-range-4", "Smaller than or equal to '<=' operator")>]
    let ``NumericRange Query Test 4`` (client : IFlexClient) = 
        let expected = indexData.Where(fun x -> x.Population <= 1000000L).Count()
        client |> Query "population <= '1000000'" expected 48
    
    [<Theory; RestData; Example("post-indices-search-highlighting-1", "Text highlighting example")>]
    let ``Search Highlight Feature Test1`` (client : IFlexClient) = 
        let query = new SearchQuery("country", "background = 'most prosperous countries'")
        let highlight = new List<string>()
        highlight.Add("background")
        query.Highlights <- new HighlightOption(highlight)
        query.Highlights.FragmentsToReturn <- 2
        query.Columns.Add("country")
        query.Columns.Add("background")
        let result = client.Search(query).Result
        result |> ExpectSuccess
        Assert.True(result.Data.Documents.Count > 0)
