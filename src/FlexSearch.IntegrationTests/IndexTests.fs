namespace FlexSearch.IntegrationTests

open Autofac
open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.TestSupport
open Ploeh.AutoFixture.Xunit
open System.Collections.Generic
open System.Linq
open Xunit
open Xunit.Extensions

module ``Basic index operation tests`` = 
    [<Fact>]
    let ``Dummy Test to get ncrunch working``() = Assert.Equal(1, 1)
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``It is not possible to close an closed index`` (indexService : IIndexService, index : Index) = 
        index.Online <- false
        indexService.AddIndex(index) |> ExpectSuccess
        indexService.CloseIndex(index.IndexName) |> ExpectErrorCode(INDEX_IS_ALREADY_OFFLINE |> GenerateOperationMessage)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Can not create the same index twice`` (indexService : IIndexService, index : Index) = 
        index.Online <- false
        indexService.AddIndex(index) |> ExpectSuccess
        indexService.AddIndex(index) |> ExpectErrorCode(INDEX_ALREADY_EXISTS |> GenerateOperationMessage)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``It is not possible to open an opened index`` (indexService : IIndexService, index : Index) = 
        index.Online <- true
        indexService.AddIndex(index) |> ExpectSuccess
        indexService.OpenIndex(index.IndexName) |> ExpectErrorCode(INDEX_IS_OPENING |> GenerateOperationMessage)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Newly created index should be online`` (indexService : IIndexService, index : Index) = 
        index.Online <- true
        indexService.AddIndex(index) |> ExpectSuccess
        indexService.GetIndexStatus(index.IndexName) |> TestSuccess IndexState.Online
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Newly created index should be offline`` (indexService : IIndexService, index : Index) = 
        index.Online <- false
        indexService.AddIndex(index) |> ExpectSuccess
        indexService.GetIndexStatus(index.IndexName) |> TestSuccess IndexState.Offline
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Offline index can be made online`` (indexService : IIndexService, index : Index) = 
        index.Online <- false
        indexService.AddIndex(index) |> ExpectSuccess
        indexService.OpenIndex(index.IndexName) |> ExpectSuccess
        indexService.GetIndexStatus(index.IndexName) |> TestSuccess IndexState.Online
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Online index can be made offline`` (indexService : IIndexService, index : Index) = 
        index.Online <- true
        indexService.AddIndex(index) |> ExpectSuccess
        indexService.CloseIndex(index.IndexName) |> ExpectSuccess
        indexService.GetIndexStatus(index.IndexName) |> TestSuccess IndexState.Offline
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess

module ``Dynamic field tests`` = 
    let testData = """
id,topic,givenname,surname,cvv2
1,a,aron,jhonson,1
2,c,steve,hewitt,1
3,b,george,Garner,1
4,e,jhon,Garner,1
5,d,simon,jhonson,1"""
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Searching is possible on the dynamic field`` (index : Index, indexService : IIndexService, 
                                                        documentService : IDocumentService, 
                                                        searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "fullname eq 'aron jhonson'")
        query.Columns.Add("fullname")
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<string>("aron jhonson", result.Documents.[0].Fields.["fullname"])
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
