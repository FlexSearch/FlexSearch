namespace FlexSearch.IntegrationTests

open Autofac
open FlexSearch.Api
open FlexSearch.Api.Message
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
        indexService.CloseIndex(index.IndexName) |> ExpectErrorCode(MessageConstants.INDEX_IS_ALREADY_OFFLINE)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Can not create the same index twice`` (indexService : IIndexService, index : Index) = 
        index.Online <- false
        indexService.AddIndex(index) |> ExpectSuccess
        indexService.AddIndex(index) |> ExpectErrorCode(MessageConstants.INDEX_ALREADY_EXISTS)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess

    [<Theory>][<AutoMockIntegrationData>]
    let ``It is not possible to open an opened index`` (indexService : IIndexService, index : Index) = 
        index.Online <- true
        indexService.AddIndex(index) |> ExpectSuccess
        indexService.OpenIndex(index.IndexName) |> ExpectErrorCode(MessageConstants.INDEX_IS_OPENING)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess


//          testCase "Newly created index should be online" <| fun _ -> 
//              let index = Helpers.GetBasicIndexSettingsForContact()
//              index.Online <- true
//              Helpers.nodeState |> IndexService.AddIndex(index) |> ignore
//              Helpers.nodeState |> IndexService.GetIndexStatus(index.IndexName) 
//              |> Helpers.expectedSuccessMessage IndexState.Online
//              Helpers.nodeState |> IndexService.DeleteIndex(index.IndexName) |> ignore
//          testCase "Newly created index should be offline" <| fun _ -> 
//              let index = Helpers.GetBasicIndexSettingsForContact()
//              index.Online <- false
//              Helpers.nodeState |> IndexService.AddIndex(index) |> ignore
//              Helpers.nodeState |> IndexService.GetIndexStatus(index.IndexName) 
//              |> Helpers.expectedSuccessMessage IndexState.Offline
//              Helpers.nodeState |> IndexService.DeleteIndex(index.IndexName) |> ignore
//          testCase "Offline index can be made online" <| fun _ -> 
//              let index = Helpers.GetBasicIndexSettingsForContact()
//              index.Online <- false
//              Helpers.nodeState |> IndexService.AddIndex(index) |> ignore
//              Helpers.nodeState |> IndexService.OpenIndex(index.IndexName) |> ignore
//              Helpers.nodeState |> IndexService.GetIndexStatus(index.IndexName) 
//              |> Helpers.expectedSuccessMessage IndexState.Online
//              Helpers.nodeState |> IndexService.DeleteIndex(index.IndexName) |> ignore
//          testCase "Online index can be made offline" <| fun _ -> 
//              let index = Helpers.GetBasicIndexSettingsForContact()
//              index.Online <- true
//              Helpers.nodeState |> IndexService.AddIndex(index) |> ignore
//              Helpers.nodeState |> IndexService.CloseIndex(index.IndexName) |> ignore
//              Helpers.nodeState |> IndexService.GetIndexStatus(index.IndexName) 
//              |> Helpers.expectedSuccessMessage IndexState.Offline
//              Helpers.nodeState |> IndexService.DeleteIndex(index.IndexName) |> ignore ]
//
//[<Tests>]
//let dynamicFieldTests() = 
//    let testData = """
//id,topic,givenname,surname,cvv2
//1,a,aron,jhonson,1
//2,c,steve,hewitt,1
//3,b,george,Garner,1
//4,e,jhon,Garner,1
//5,d,simon,jhonson,1
//"""
//    let index = Helpers.GetBasicIndexSettingsForContact()
//    Helpers.nodeState |> IndexService.AddIndex(index) |> ignore
//    Helpers.AddTestDataToIndex(index, testData)
//    let query = new SearchQuery(index.IndexName, "fullname eq 'aron jhonson'")
//    let result = ref Unchecked.defaultof<SearchResults>
//    testList "Dynamic field generation tests" 
//        [ testCase "Searching is possible on the dynamic field" <| fun _ -> 
//              query.Columns.Add("fullname")
//              query.IndexName <- index.IndexName
//              result 
//              := Helpers.getResult 
//                     (Helpers.nodeState |> SearchService.Search(query))
//              result.Value.Documents.[0].Fields.["fullname"] |> should equal "aron jhonson"
//          
//          testCase "Cleanup" 
//          <| fun _ -> 
//              Helpers.nodeState |> IndexService.DeleteIndex(index.IndexName) |> ignore ]
