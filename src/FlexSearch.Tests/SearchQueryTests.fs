module SearchQueryTests

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
open System.Collections.Generic
open System.Linq

type QueryTestObject = 
    { Note : string
      Input : string
      Output : int }

type SearchTest = 
    { Title : string
      TestData : string
      TestObjects : QueryTestObject list }

let search indexName testObject () = 
    let query = new SearchQuery(indexName, testObject.Input)
    (Helpers.getResult (Helpers.indexService.PerformQuery(indexName, query))).RecordsReturned 
    |> should equal testObject.Output
    ()

let SearchTestRunner(tests : SearchTest) = 
    let index = Helpers.GetBasicIndexSettingsForContact()
    let result = Helpers.indexService.AddIndex(index)
    Helpers.AddTestDataToIndex(Helpers.indexService, index, tests.TestData)
    testList tests.Title 
        [ for test in tests.TestObjects -> testCase test.Note (search index.IndexName test)
          yield testCase "Cleanup" <| fun _ -> Helpers.indexService.DeleteIndex(index.IndexName) |> ignore ]

[<Tests>]
let phraseMatchTests = 
    let testData = """
id,topic,abstract
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
"""
    
    let testCases = 
        [ { Note = "Searching for 'practical approach' with a slop of 1 will return 1 result"
            Input = "abstract match ['practical approach', 'slop:1']"
            Output = 1 }
          { Note = "Searching for 'practical approach' with a default slop of 1 will return 1 result"
            Input = "abstract match 'practical approach'"
            Output = 1 }
          { Note = "Searching for 'approach practical' will not return anything as the order matters"
            Input = "abstract match 'approach practical'"
            Output = 0 }
          { Note = "Searching for 'approach computation' with a slop of 2 will return 1 result"
            Input = "abstract match ['approach computation', 'slop:2']"
            Output = 1 }
          { Note = "Searching for 'comprehensive process leads' with a slop of 1 will return 1 result"
            Input = "abstract match ['comprehensive process leads', 'slop:1']"
            Output = 1 } ]
    
    let tests = 
        { Title = "Phrase Match tests"
          TestData = testData
          TestObjects = testCases }
    
    SearchTestRunner tests

[<Tests>]
let termMatchSimpleTests = 
    let testData = """
id,givenname,surname,cvv2
1,Aaron,jhonson,23
2,aaron,hewitt,32
3,Fred,Garner,44
4,aaron,Garner,43
5,fred,jhonson,332
"""
    
    let testCases = 
        [ { Note = "Searching for 'id eq 1' should return 1 records"
            Input = "_id eq '1'"
            Output = 1 }
          { Note = "Searching for int field 'cvv2 eq 44' should return 1 records"
            Input = "cvv2 eq '44'"
            Output = 1 }
          { Note = "Searching for 'aaron' should return 3 records"
            Input = "givenname eq 'aaron'"
            Output = 3 }
          { Note = "Searching for 'aaron' & 'jhonson' should return 1 record"
            Input = "givenname eq 'aaron' and surname eq 'jhonson'"
            Output = 1 }
          { Note = "Searching for givenname 'aaron' & surname 'jhonson or Garner' should return 2 record"
            Input = "givenname eq 'aaron' and (surname eq 'jhonson' or surname eq 'Garner')"
            Output = 2 }
          { Note = "Searching for 'id eq 1' should return 1 records"
            Input = "_id = '1'"
            Output = 1 }
          { Note = "Searching for int field 'cvv2 eq 44' should return 1 records"
            Input = "cvv2 = '44'"
            Output = 1 }
          { Note = "Searching for 'aaron' should return 3 records"
            Input = "givenname = 'aaron'"
            Output = 3 }
          { Note = "Searching for 'aaron' & 'jhonson' should return 1 record"
            Input = "givenname = 'aaron' and surname = 'jhonson'"
            Output = 1 }
          { Note = "Searching for givenname 'aaron' & surname 'jhonson or Garner' should return 2 record"
            Input = "givenname = 'aaron' and (surname = 'jhonson' or surname = 'Garner')"
            Output = 2 } ]
    
    let tests = 
        { Title = "Term Match tests (Simple)"
          TestData = testData
          TestObjects = testCases }
    
    SearchTestRunner tests

[<Tests>]
let termMatchComplexTests = 
    let testData = """
id,topic,abstract
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
"""
    
    let testCases = 
        [ { Note = 
                "Searching for multiple words will create a new query which will search all the words but not in specific order"
            Input = "abstract eq 'CompSci abbreviated approach'"
            Output = 1 }
          { Note = 
                "Searching for multiple words will create a new query which will search all the words using AND style construct but not in specific order"
            Input = "abstract eq 'CompSci abbreviated approach undefinedword'"
            Output = 0 }
          { Note = 
                "Setting 'clausetype' in condition properties can override the default clause construction from AND style to OR"
            Input = "abstract eq ['CompSci abbreviated approach undefinedword' , 'clausetype:or']"
            Output = 1 } ]
    
    let tests = 
        { Title = "Term Match tests (Complex)"
          TestData = testData
          TestObjects = testCases }
    
    SearchTestRunner tests

[<Tests>]
let fuzzyWildCardMatchTests = 
    let testData = """
id,givenname,surname,cvv2
1,Aaron,jhonson,23
2,aron,hewitt,32
3,Airon,Garner,44
4,aroon,Garner,43
5,aronn,jhonson,332
6,aroonn,jhonson,332
7,boat,,jhonson,332
8,moat,jhonson,332
"""
    
    let testCases = 
        [ { Note = "Searching for 'givenname = aron' with default slop of 1 should return 5 records"
            Input = "givenname fuzzy 'aron'"
            Output = 5 }
          { Note = "Searching for 'givenname = aron' with specified slop of 1 should return 5 records"
            Input = "givenname fuzzy ['aron', 'slop:1']"
            Output = 5 }
          { Note = "Searching for 'givenname = aron' with slop of 2 should return 6 records"
            Input = "givenname fuzzy ['aron', 'slop:2']"
            Output = 6 }
          { Note = "Searching for 'givenname = aron' with default slop of 1 should return 5 records"
            Input = "givenname ~= 'aron'"
            Output = 5 }
          { Note = "Searching for 'givenname = aron' with specified slop of 1 should return 5 records"
            Input = "givenname ~= ['aron', 'slop:1']"
            Output = 5 }
          { Note = "Searching for 'givenname = aron' with slop of 2 should return 6 records"
            Input = "givenname ~= ['aron', 'slop:2']"
            Output = 6 }
          { Note = "Searching for 'givenname = aron?' should return 1 records"
            Input = "givenname like 'aron?'"
            Output = 1 }
          { Note = "Searching for 'givenname = aron*' should return 2 records"
            Input = "givenname like 'aron*'"
            Output = 2 }
          { Note = "Searching for 'givenname = ar?n' should return 1 records"
            Input = "givenname like 'ar?n'"
            Output = 1 }
          { Note = 
                "Searching for 'givenname = AR?N' should return 0 records as matching is case sensitive and like bypasses analysis"
            Input = "givenname like 'AR?N'"
            Output = 0 }
          { Note = "Searching for 'givenname = aron?' should return 1 records"
            Input = "givenname %= 'aron?'"
            Output = 1 }
          { Note = "Searching for 'givenname = aron*' should return 2 records"
            Input = "givenname %= 'aron*'"
            Output = 2 }
          { Note = "Searching for 'givenname = ar?n' should return 1 records"
            Input = "givenname %= 'ar?n'"
            Output = 1 }
          { Note = 
                "Searching for 'givenname = AR?N' should return 0 records as matching is case sensitive and like bypasses analysis"
            Input = "givenname %= 'AR?N'"
            Output = 0 }
          { Note = "Searching for 'givenname = [mb]oat' should return 2 records"
            Input = "givenname regex '[mb]oat'"
            Output = 2 } ]
    
    let tests = 
        { Title = "Fuzzy/Wildcard tests"
          TestData = testData
          TestObjects = testCases }
    
    SearchTestRunner tests

[<Tests>]
let rangeQueryTests = 
    let testData = """
id,givenname,surname,cvv2
1,Aaron,jhonson,1
2,aaron,hewitt,5
3,Fred,Garner,10
4,aaron,Garner,15
5,fred,jhonson,20
"""
    
    let testCases = 
        [ { Note = 
                "Searching for records with cvv in range 1 to 20 inclusive upper & lower bound should return 5 records"
            Input = "cvv2 >= '1' and cvv2 <= '20'"
            Output = 5 }
          { Note = 
                "Searching for records with cvv in range 1 to 20 exclusive upper & lower bound should return 3 records"
            Input = "cvv2 > '1' and cvv2 < '20'"
            Output = 3 }
          { Note = 
                "Searching for records with cvv in range 1 to 20 inclusive upper & exclusive lower bound should return 4 records"
            Input = "cvv2 >= '1' and cvv2 < '20'"
            Output = 4 }
          { Note = 
                "Searching for records with cvv in range 1 to 20 excluding upper & including lower bound should return 4 records"
            Input = "cvv2 > '1' and cvv2 <= '20'"
            Output = 4 }
          { Note = "Searching for records with cvv2 > '1' should return 4"
            Input = "cvv2 > '1'"
            Output = 4 }
          { Note = "Searching for records with cvv2 >= '1' should return 5"
            Input = "cvv2 >= '1'"
            Output = 5 }
          { Note = "Searching for records with cvv2 < '20' should return 4"
            Input = "cvv2 < '20'"
            Output = 4 }
          { Note = "Searching for records with cvv2 <= '20' should return 5"
            Input = "cvv2 <= '20'"
            Output = 5 } ]
    
    let tests = 
        { Title = "Range query tests"
          TestData = testData
          TestObjects = testCases }
    
    SearchTestRunner tests
