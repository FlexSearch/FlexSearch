module IndexTests

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
open System.Collections.Generic
open System.Linq

//[<Tests>]
//let indexCreationTests() = 
//    testList "Basic index operation tests" 
//        [ testCase "It is not possible to close an closed index" <| fun _ -> 
//              
//              let index = Helpers.GetBasicIndexSettingsForContact()
//              index.Online <- false
//              let result = Helpers.nodeState |> IndexService.AddIndex(index)
//              Helpers.nodeState |> IndexService.CloseIndex(index.IndexName) 
//              |> Helpers.expectedFailureMessage 
//                     (MessageConstants.INDEX_IS_ALREADY_OFFLINE)
//              Helpers.nodeState |> IndexService.DeleteIndex(index.IndexName) |> ignore
//          testCase "Can not create the same index twice" <| fun _ -> 
//              let index = Helpers.GetBasicIndexSettingsForContact()
//              Helpers.nodeState |> IndexService.AddIndex(index) |> ignore
//              Helpers.nodeState |> IndexService.AddIndex(index) 
//              |> Helpers.expectedFailureMessage 
//                     (MessageConstants.INDEX_ALREADY_EXISTS)
//              Helpers.nodeState |> IndexService.DeleteIndex index.IndexName |> ignore
//          testCase "It is not possible to open an opened index" <| fun _ -> 
//              let index = Helpers.GetBasicIndexSettingsForContact()
//              index.Online <- true
//              Helpers.nodeState |> IndexService.AddIndex(index) |> ignore
//              Helpers.nodeState |> IndexService.OpenIndex(index.IndexName) 
//              |> Helpers.expectedFailureMessage 
//                     (MessageConstants.INDEX_IS_OPENING)
//              Helpers.nodeState |> IndexService.DeleteIndex(index.IndexName) |> ignore
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
