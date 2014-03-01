module SearchTests

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
open System.Collections.Generic
open System.Linq

let test (f : Choice<SearchResults, OperationMessage>) = 
    match f with
    | Choice1Of2(a) -> a
    | Choice2Of2(b) -> failtest b.DeveloperMessage

[<Tests>]
let columnTests = 
    let testData = """
id,topic,surname,cvv2
1,a,jhonson,1
2,c,hewitt,1
3,b,Garner,1
4,e,Garner,1
5,d,jhonson,1
"""
    let index = Helpers.GetBasicIndexSettingsForContact()
    let result = Helpers.indexService.AddIndex(index)
    Helpers.AddTestDataToIndex(Helpers.indexService, index, testData)
    let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
    let result = ref Unchecked.defaultof<SearchResults>
    testList "Search results columns test" 
        [ testCase "Searching with no columns specified will return no additional columns" <| fun _ -> 
              result := test (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.Value.Documents.[0].Fields.Count |> should equal 0
          testCase "Searching with columns specified with '*' will return all column" <| fun _ -> 
              query.Columns.Add("*")
              result := test (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.contents.Documents.[0].Fields.Count |> should equal index.Fields.Count
          
          testCase "The returned columns should contain column 'topic'" 
          <| fun _ -> result.contents.Documents.[0].Fields.ContainsKey("topic") |> should equal true
          
          testCase "The returned columns should contain column 'surname'" 
          <| fun _ -> result.contents.Documents.[0].Fields.ContainsKey("surname") |> should equal true
          
          testCase "The returned columns should contain column 'cvv2'" 
          <| fun _ -> result.contents.Documents.[0].Fields.ContainsKey("cvv2") |> should equal true
          testCase "Searching with columns specified as 'topic' will return just one column" <| fun _ -> 
              query.Columns.Clear()
              query.Columns.Add("topic")
              result := test (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.contents.Documents.[0].Fields.Count |> should equal 1
          
          testCase "The returned columns should be 'topic'" 
          <| fun _ -> result.contents.Documents.[0].Fields.ContainsKey("topic") |> should equal true
          testCase "Searching with columns specified as 'topic' & 'surname' will return just two columns" <| fun _ -> 
              query.Columns.Clear()
              query.Columns.Add("topic")
              query.Columns.Add("surname")
              result := test (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.contents.Documents.[0].Fields.Count |> should equal 2
          
          testCase "The returned columns should contain column 'topic'" 
          <| fun _ -> result.contents.Documents.[0].Fields.ContainsKey("topic") |> should equal true
          
          testCase "The returned columns should contain column 'surname'" 
          <| fun _ -> result.contents.Documents.[0].Fields.ContainsKey("surname") |> should equal true
          testCase "Cleanup" <| fun _ -> Helpers.indexService.DeleteIndex(index.IndexName) |> ignore ]

[<Tests>]
let pagingTests = 
    let testData = """
id,givenname,surname,cvv2
1,Aaron,jhonson,1
2,aron,hewitt,1
3,Airon,Garner,1
4,aroon,Garner,1
5,aronn,jhonson,1
6,aroonn,jhonson,1
"""
    let index = Helpers.GetBasicIndexSettingsForContact()
    let result = Helpers.indexService.AddIndex(index)
    Helpers.AddTestDataToIndex(Helpers.indexService, index, testData)
    let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
    let result = ref Unchecked.defaultof<SearchResults>
    testList "Search results paging tests" 
        [ testCase "Searching for 'cvv2 = 1' with Count = 2 will return 2 records" <| fun _ -> 
              query.Count <- 2
              result := test (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.Value.Documents.Count |> should equal 2
          testCase "First record will be with id = 1" <| fun _ -> result.contents.Documents.[0].Id |> should equal "1"
          testCase "Second record will be with id = 2" <| fun _ -> result.contents.Documents.[1].Id |> should equal "2"
          testCase "Searching for 'cvv2 = 1' with records to return = 2 and skip = 2 will return 2 records" <| fun _ -> 
              query.Count <- 2
              query.Skip <- 2
              result := test (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.Value.Documents.Count |> should equal 2
          testCase "First record will be with id = 3" <| fun _ -> result.contents.Documents.[0].Id |> should equal "3"
          testCase "Second record will be with id = 4" <| fun _ -> result.contents.Documents.[1].Id |> should equal "4"
          testCase "Searching for 'cvv2 = 1' with records to return = 2 and skip = 3 will return 2 records" <| fun _ -> 
              query.Count <- 2
              query.Skip <- 3
              result := test (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.Value.Documents.Count |> should equal 2
          testCase "First record will be with id = 4" <| fun _ -> result.contents.Documents.[0].Id |> should equal "4"
          testCase "Second record will be with id = 5" <| fun _ -> result.contents.Documents.[1].Id |> should equal "5"
          testCase "Cleanup" <| fun _ -> Helpers.indexService.DeleteIndex(index.IndexName) |> ignore ]

[<Tests>]
let simpleSortingTests = 
    let testData = """
id,topic,surname,cvv2
1,a,jhonson,1
2,c,hewitt,1
3,b,Garner,1
4,e,Garner,1
5,d,jhonson,1
"""
    let index = Helpers.GetBasicIndexSettingsForContact()
    let result = Helpers.indexService.AddIndex(index)
    Helpers.AddTestDataToIndex(Helpers.indexService, index, testData)
    let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
    let result = ref Unchecked.defaultof<SearchResults>
    testList "Search results sorting tests" 
        [ testCase "Searching for 'cvv2 = 1' with orderby topic should return 5 records" <| fun _ -> 
              query.OrderBy <- "topic"
              query.Columns.Add("topic")
              result := test (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.Value.Documents.Count |> should equal 5
          
          testCase "1st record should be a" 
          <| fun _ -> result.contents.Documents.[0].Fields.["topic"] |> should equal "a"
          
          testCase "2nd record should be a" 
          <| fun _ -> result.contents.Documents.[1].Fields.["topic"] |> should equal "b"
          
          testCase "3rd record should be a" 
          <| fun _ -> result.contents.Documents.[2].Fields.["topic"] |> should equal "c"
          
          testCase "4th record should be a" 
          <| fun _ -> result.contents.Documents.[3].Fields.["topic"] |> should equal "d"
          
          testCase "5th record should be a" 
          <| fun _ -> result.contents.Documents.[4].Fields.["topic"] |> should equal "e"
          testCase "Cleanup" <| fun _ -> Helpers.indexService.DeleteIndex(index.IndexName) |> ignore ]

[<Tests>]
let simpleHighlightingTests = 
    let testData = """
id,topic,abstract
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
"""
    let index = Helpers.GetBasicIndexSettingsForContact()
    let result = Helpers.indexService.AddIndex(index)
    Helpers.AddTestDataToIndex(Helpers.indexService, index, testData)
    let query = new SearchQuery(index.IndexName, "abstract match 'practical approach'")
    let result = ref Unchecked.defaultof<SearchResults>
    testList "Search results highlighting tests" 
        [ testCase "Searching for 'practical approach' with highlighting will return 1 result" <| fun _ -> 
              query.Highlights <- new HighlightOption(new List<string>([ "abstract" ]))
              query.Highlights.FragmentsToReturn <- 1
              query.Highlights.PreTag <- "<imp>"
              query.Highlights.PostTag <- "</imp>"
              result := test (Helpers.indexService.PerformQuery(index.IndexName, query))
              result.Value.Documents.Count |> should equal 1
          
          testCase "It will return a highlighted passage" 
          <| fun _ -> result.contents.Documents.[0].Highlights.Count |> should equal 1
          
          testCase "The highlighted passage should contain 'practical'" 
          <| fun _ -> result.contents.Documents.[0].Highlights.[0].Contains("practical") |> should equal true
          
          testCase "The highlighted passage should contain 'approach'" 
          <| fun _ -> result.contents.Documents.[0].Highlights.[0].Contains("approach") |> should equal true
          
          testCase "The highlighted should contain 'practical' with in pre and post tags" 
          <| fun _ -> result.contents.Documents.[0].Highlights.[0].Contains("<imp>practical</imp>") |> should equal true
          
          testCase "The highlighted should contain 'approach' with in pre and post tags" 
          <| fun _ -> result.contents.Documents.[0].Highlights.[0].Contains("<imp>approach</imp>") |> should equal true
          testCase "Cleanup" <| fun _ -> Helpers.indexService.DeleteIndex(index.IndexName) |> ignore ]
