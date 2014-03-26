module HttpModuleTests

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
open HttpClient
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Threading

// Test server
//let serverSettings = GetServerSettings(ConfFolder.Value + "\\Config.json")
//let node = new NodeService(serverSettings, true)
//
//node.Start()
//
//let responseStatusEquals status (response : Response) = 
//    testCase (sprintf "Should return %i" status) <| fun _ -> response.StatusCode |> should equal status
//let responseContainsHeader (header : ResponseHeader) (value : string) (response : Response) = 
//    testCase "Should contain header" <| fun _ -> response.Headers.[header] |> should equal value
//
//let responseMatches (message : string) (select : string) (expected : string) (response : Response) = 
//    testCase message <| fun _ -> 
//        let value = JObject.Parse(response.EntityBody.Value)
//        value.SelectToken(select).ToString() |> should equal expected
//
//let responseShouldContain (value : string) (response : Response) = 
//    testCase "Response should contain" <| fun _ -> 
//        value.Contains(value) |> should equal true
//
//let responseContainsProperty (message : string) (group : string) (key: string) (property: string) (expected : string) (response : Response) = 
//    testCase message <| fun _ -> 
//        let value = JObject.Parse(response.EntityBody.Value)
//        value.SelectToken(group).[key].[property].ToString() |> should equal expected
//
//
//let responseBodyIsNull (response : Response) = 
//    testCase "Should not have any response body" <| fun _ -> response.EntityBody |> should equal None
//let httpUrl = "http://seemant-pc:9800"
//let response = ref Unchecked.defaultof<Response>
//
//[<Tests>]
//let indexCreateTest1 () = 
//    testList "A new index can be successfully created." [ 
//        let index = Helpers.GetBasicIndexSettingsForContact()
//        index.IndexName <- "testindex"
//        response := createRequest Post (sprintf "%s/testindex" httpUrl)
//                    |> withBody (JsonConvert.SerializeObject(index))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseBodyIsNull
//    ]
//
//[<Tests>]
//let indexCreateTest2 () = 
//    testList "Duplicate index cannot be created." [ 
//        let index = Helpers.GetBasicIndexSettingsForContact()
//        index.IndexName <- "testindex"
//        response := createRequest Post (sprintf "%s/testindex" httpUrl)
//                    |> withBody (JsonConvert.SerializeObject(index))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 400
//        yield !response |> responseMatches "Should return index already exists" "ErrorCode" "1002"
//    ]
//
//[<Tests>]
//let indexCreateTest3 () = 
//    testList "Newly created index can be updated." [ 
//        let index = Helpers.GetBasicIndexSettingsForContact()
//        index.IndexName <- "testindex"
//        index.Fields.Add("dummy", new FieldProperties(FieldType = FieldType.Stored))
//        response := createRequest Put (sprintf "%s/testindex" httpUrl)
//                    |> withBody (JsonConvert.SerializeObject(index))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseBodyIsNull
//
//        // Access the updated index settings
//        response := createRequest Get (sprintf "%s/testindex" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response 
//            |> responseMatches "Should contain the indexname in the response" "IndexName" 
//                    "testindex"
//        yield !response 
//            |> responseContainsProperty "Should contain the field dummy in the response" "Fields" "dummy" "FieldType"
//                    "Stored"
//    ]
//
//[<Tests>]
//let indexCreateTest4 () = 
//    testList "Index can be accessed without provding accepttype notation." [ 
//        response := createRequest Get (sprintf "%s/testindex" httpUrl)
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response 
//            |> responseMatches "Should contain the indexname in the response" "IndexName" 
//                    "testindex"
//        yield !response 
//            |> responseContainsProperty "Should contain the field dummy in the response" "Fields" "dummy" "FieldType"
//                    "Stored"
//    ]
//
//
////[<Tests>]
////let indexCreateTest5 () = 
////    testList "Index creation requests without body will return error." [ 
////        response := createRequest Post (sprintf "%s/testindex" httpUrl)
////                    |> withHeader (ContentType "application/json")
////                    |> withBody ""
////                    |> getResponse
////        yield !response |> responseStatusEquals 400
////        yield !response |> responseMatches "Should return body expected error" "ErrorCode" "6002"
////        response := createRequest Put (sprintf "%s/testindex" httpUrl)
////                    |> withHeader (ContentType "application/json")
////                    |> withBody ""
////                    |> getResponse
////        yield !response |> responseStatusEquals 400
////        yield !response |> responseMatches "Should return body expected error" "ErrorCode" "6002"
////    ]
//
//[<Tests>]
//let indexCreateTest6 () = 
//    testList "Put request with wrong indexname will return an error." [ 
//        let index = Helpers.GetBasicIndexSettingsForContact()
//        index.IndexName <- "testindex"
//        response := createRequest Put (sprintf "%s/testindex123" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> withBody (JsonConvert.SerializeObject(index))
//                    |> getResponse
//        yield !response |> responseStatusEquals 400 
//        yield !response |> responseMatches "Expecting error code 1000" "ErrorCode" "1000"
//    ]
//
//[<Tests>]
//let indexStatusTest1 () = 
//    testList "Created index should be online." [ 
//        response := createRequest Get (sprintf "%s/testindex/status" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200 
//        yield !response |> responseMatches "Expecting status 'Online'" "Status" "Online"
//    ]
//
//[<Tests>]
//let indexStatusTest2 () = 
//    testList "Index can be made offline" [ 
//        response := createRequest Post (sprintf "%s/testindex/status/offline" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> withBody ""
//                    |> getResponse
//        yield !response |> responseStatusEquals 200 
//        response := createRequest Get (sprintf "%s/testindex/status" httpUrl)
//                |> withHeader (ContentType "application/json")
//                |> getResponse
//        yield !response |> responseStatusEquals 200 
//        yield !response |> responseMatches "Index status should be offline." "Status" "Offline"
//    ]
//
//[<Tests>]
//let indexStatusTest3 () = 
//    testList "Index can be made online" [ 
//        response := createRequest Post (sprintf "%s/testindex/status/online" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> withBody ""
//                    |> getResponse
//        yield !response |> responseStatusEquals 200 
//        response := createRequest Get (sprintf "%s/testindex/status" httpUrl)
//                |> withHeader (ContentType "application/json")
//                |> getResponse
//        yield !response |> responseStatusEquals 200 
//        yield !response |> responseMatches "Index status should be online." "Status" "Online"
//    ]
//
//[<Tests>]
//let indexExistTest1 () = 
//    testList "Index exist tests" [ 
//        response := createRequest Get (sprintf "%s/testindex/exists" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200 
//        response := createRequest Get (sprintf "%s/testindex123/exists" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 400
//    ]
//
//[<Tests>]
//let documentsTest1 () = 
//    testList "Documents can be added to an existing index" [ 
//        let document1 = dict [ ("topic", "a")
//                               ("surname", "jhonson")
//                               ("cvv2", "1") ]
//        
//        response := createRequest Post (sprintf "%s/testindex/documents/1" httpUrl)
//                    |> withBody (JsonConvert.SerializeObject(document1))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        // Let's wait so that the document is available for searching
//        Thread.Sleep(2000)
//        // We can get the newly indexed document back
//        response := createRequest Get (sprintf "%s/testindex/documents/1" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseMatches "Records returned should be 1." "RecordsReturned" "1"
//        yield !response |> responseMatches "Total available should be 1" "TotalAvailable" "1"
//    ]
//
//[<Tests>]
//let documentsTest2 () = 
//    testList "Documents can be added to an existing index" [ 
//        let document1 = dict [ ("topic", "b")
//                               ("surname", "rajvanshi")
//                               ("cvv2", "1") ]
//        
//        response := createRequest Post (sprintf "%s/testindex/documents/2" httpUrl)
//                    |> withBody (JsonConvert.SerializeObject(document1))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        // Let's wait so that the document is available for searching
//        Thread.Sleep(2000)
//        // We can get the newly indexed document back
//        response := createRequest Get (sprintf "%s/testindex/documents" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseMatches "Records returned should be 2." "RecordsReturned" "2"
//        yield !response |> responseMatches "Total available should be 2" "TotalAvailable" "2"
//    ]
//
//[<Tests>]
//let searchTest1 () = 
//    testList "Searching document by surname is possible" [ 
//        let query = new SearchQuery("testindex", "surname = 'rajvanshi'")
//        
//        response := createRequest Post (sprintf "%s/testindex/search" httpUrl)
//                    |> withBody (JsonConvert.SerializeObject(query))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseMatches "Records returned should be 1" "RecordsReturned" "1"
//        yield !response |> responseMatches "Total available should be 1" "TotalAvailable" "1"
//
//        query.ReturnFlatResult <- true
//        response := createRequest Post (sprintf "%s/testindex/search" httpUrl)
//                    |> withBody (JsonConvert.SerializeObject(query))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseContainsHeader  (NonStandard("RecordsReturned")) "1"
//        yield !response |> responseContainsHeader (NonStandard("TotalAvailable")) "1"
//    ]
//
//[<Tests>]
//let searchTestParameters1 () = 
//    testList "Search query parameters tests: Setting q in querystring" [ 
//        let query = new SearchQuery()
//        response := createRequest Post (sprintf "%s/testindex/search" httpUrl)
//                    |> withQueryStringItem {name="q"; value="surname = 'rajvanshi'"}
//                    |> withQueryStringItem {name="Returnflatresult"; value="true"}
//                    |> withBody (JsonConvert.SerializeObject(query))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseContainsHeader  (NonStandard("RecordsReturned")) "1"
//        yield !response |> responseContainsHeader (NonStandard("TotalAvailable")) "1"
//    ]
//
//[<Tests>]
//let searchTestParameters2 () = 
//    testList "Search query string parameters tests: Setting c in querystring" [ 
//        let query = new SearchQuery()
//        response := createRequest Post (sprintf "%s/testindex/search" httpUrl)
//                    |> withQueryStringItem {name="q"; value="surname = 'rajvanshi'"}
//                    |> withQueryStringItem {name="c"; value="surname,topic"}
//                    |> withQueryStringItem {name="Returnflatresult"; value="true"}
//                    |> withBody (JsonConvert.SerializeObject(query))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseContainsHeader  (NonStandard("RecordsReturned")) "1"
//        yield !response |> responseContainsHeader (NonStandard("TotalAvailable")) "1"
//        yield !response |> responseShouldContain "topic"
//        yield !response |> responseShouldContain "surname"
//    ]
//
//[<Tests>]
//let searchTestParameters3 () = 
//    testList "Search query string parameters tests: Setting count in querystring" [ 
//        let query = new SearchQuery()
//        response := createRequest Post (sprintf "%s/testindex/search" httpUrl)
//                    |> withQueryStringItem {name="q"; value="surname = 'rajvanshi'"}
//                    |> withQueryStringItem {name="c"; value="surname,topic"}
//                    |> withQueryStringItem {name="count"; value="1"}
//                    |> withQueryStringItem {name="Returnflatresult"; value="true"}
//                    |> withBody (JsonConvert.SerializeObject(query))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseContainsHeader  (NonStandard("RecordsReturned")) "1"
//        yield !response |> responseContainsHeader (NonStandard("TotalAvailable")) "1"
//        yield !response |> responseShouldContain "topic"
//        yield !response |> responseShouldContain "surname"
//    ]
//
//[<Tests>]
//let searchTestParameters4 () = 
//    testList "Search query string parameters tests: Setting count in querystring" [ 
//        let query = new SearchQuery()
//        response := createRequest Post (sprintf "%s/testindex/search" httpUrl)
//                    |> withQueryStringItem {name="q"; value="surname matchall 'rajvanshi'"}
//                    |> withQueryStringItem {name="c"; value="surname,topic"}
//                    |> withQueryStringItem {name="count"; value="1"}
//                    |> withQueryStringItem {name="Returnflatresult"; value="true"}
//                    |> withBody (JsonConvert.SerializeObject(query))
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200
//        yield !response |> responseContainsHeader  (NonStandard("RecordsReturned")) "1"
//        yield !response |> responseContainsHeader (NonStandard("TotalAvailable")) "2"
//    ]
//
//[<Tests>]
//let indexDeleteTest1 () = 
//    testList "Index can be deleted" [ 
//        response := createRequest Delete (sprintf "%s/testindex" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 200 
//        response := createRequest Get (sprintf "%s/testindex/exists" httpUrl)
//                    |> withHeader (ContentType "application/json")
//                    |> getResponse
//        yield !response |> responseStatusEquals 400
//    ]
//
//let testRunHelper () =
//    testList "Debug only runner" [
//        yield indexCreateTest1()
//        yield indexCreateTest4()
////        yield documentsTest1()
////        yield documentsTest2()
////        yield searchTestParameters4()
//    ]
