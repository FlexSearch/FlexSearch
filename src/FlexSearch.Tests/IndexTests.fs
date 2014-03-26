module IndexTests

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
open System.Collections.Generic
open System.Linq

[<Tests>]
let indexCreationTests = 
    testList "Basic index operation tests" 
        [ testCase "It is not possible to close an closed index" <| fun _ -> 
              let index = Helpers.GetBasicIndexSettingsForContact()
              index.Online <- false
              Helpers.indexService.AddIndex(index) |> ignore
              Helpers.indexService.CloseIndex(index.IndexName) 
              |> Helpers.expectedFailureMessage 
                     (MessageConstants.INDEX_IS_ALREADY_OFFLINE)
              Helpers.indexService.DeleteIndex(index.IndexName) |> ignore
          testCase "Can not create the same index twice" <| fun _ -> 
              let index = Helpers.GetBasicIndexSettingsForContact()
              Helpers.indexService.AddIndex(index) |> ignore
              Helpers.indexService.AddIndex(index) 
              |> Helpers.expectedFailureMessage 
                     (MessageConstants.INDEX_ALREADY_EXISTS)
              Helpers.indexService.DeleteIndex(index.IndexName) |> ignore
          testCase "It is not possible to open an opened index" <| fun _ -> 
              let index = Helpers.GetBasicIndexSettingsForContact()
              index.Online <- true
              Helpers.indexService.AddIndex(index) |> ignore
              Helpers.indexService.OpenIndex(index.IndexName) 
              |> Helpers.expectedFailureMessage 
                     (MessageConstants.INDEX_IS_OPENING)
              Helpers.indexService.DeleteIndex(index.IndexName) |> ignore
          testCase "Newly created index should be online" <| fun _ -> 
              let index = Helpers.GetBasicIndexSettingsForContact()
              index.Online <- true
              Helpers.indexService.AddIndex(index) |> ignore
              Helpers.indexService.IndexStatus(index.IndexName) 
              |> Helpers.expectedSuccessMessage IndexState.Online
              Helpers.indexService.DeleteIndex(index.IndexName) |> ignore
          testCase "Newly created index should be offline" <| fun _ -> 
              let index = Helpers.GetBasicIndexSettingsForContact()
              index.Online <- false
              Helpers.indexService.AddIndex(index) |> ignore
              Helpers.indexService.IndexStatus(index.IndexName) 
              |> Helpers.expectedSuccessMessage IndexState.Offline
              Helpers.indexService.DeleteIndex(index.IndexName) |> ignore
          testCase "Offline index can be made online" <| fun _ -> 
              let index = Helpers.GetBasicIndexSettingsForContact()
              index.Online <- false
              Helpers.indexService.AddIndex(index) |> ignore
              Helpers.indexService.OpenIndex(index.IndexName) |> ignore
              Helpers.indexService.IndexStatus(index.IndexName) 
              |> Helpers.expectedSuccessMessage IndexState.Online
              Helpers.indexService.DeleteIndex(index.IndexName) |> ignore
          testCase "Online index can be made offline" <| fun _ -> 
              let index = Helpers.GetBasicIndexSettingsForContact()
              index.Online <- true
              Helpers.indexService.AddIndex(index) |> ignore
              Helpers.indexService.CloseIndex(index.IndexName) |> ignore
              Helpers.indexService.IndexStatus(index.IndexName) 
              |> Helpers.expectedSuccessMessage IndexState.Offline
              Helpers.indexService.DeleteIndex(index.IndexName) |> ignore ]

[<Tests>]
let dynamicFieldTests = 
    let testData = """
id,topic,givenname,surname,cvv2
1,a,aron,jhonson,1
2,c,steve,hewitt,1
3,b,george,Garner,1
4,e,jhon,Garner,1
5,d,simon,jhonson,1
"""
    let index = Helpers.GetBasicIndexSettingsForContact()
    let result = Helpers.indexService.AddIndex(index)
    Helpers.AddTestDataToIndex(Helpers.indexService, index, testData)
    let query = new SearchQuery(index.IndexName, "fullname eq 'aron jhonson'")
    let result = ref Unchecked.defaultof<SearchResults>
    testList "Dynamic field generation tests" 
        [ testCase "Searching is possible on the dynamic field" <| fun _ -> 
              query.Columns.Add("fullname")
              result 
              := Helpers.getResult 
                     (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.Value.Documents.[0].Fields.["fullname"] |> should equal "aron jhonson"
          
          testCase "Cleanup" 
          <| fun _ -> 
              Helpers.indexService.DeleteIndex(index.IndexName) |> ignore ]
