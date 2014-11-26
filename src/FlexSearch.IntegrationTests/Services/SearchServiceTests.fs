namespace FlexSearch.IntegrationTests.Search

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.TestSupport
open System.Collections.Generic
open System.Linq
open Xunit
open Xunit.Extensions

type ``Column Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,et1,t1,i1,s1
1,a,jhonson,1,test1
2,c,hewitt,1,test2
3,b,Garner,1,test3
4,e,Garner,1,test4
5,d,jhonson,1,test5"""
    do self.TestData <- testData
    
    [<Fact>]
    member __.``Searching with no columns specified will return no additional columns``() = 
        let result = 
            __.Query("i1 eq '1'")
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(0, result.Documents.[0].Fields.Count)
    
    [<Fact>]
    member __.``Searching with columns specified with '*' will return all column``() = 
        let result = 
            __.Query("i1 eq '1'")
            |> __.AddColumns([| "*" |])
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(__.Index.Fields.Count, result.Documents.[0].Fields.Count)
    
    [<Fact>]
    member __.``Searching with columns specified as 'topic' will return just one column``() = 
        let result = 
            __.Query("i1 eq '1'")
            |> __.AddColumns([| "et1" |])
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(1, result.Documents.[0].Fields.Count)
        Assert.True(result.Documents.[0].Fields.ContainsKey("et1"))
    
    [<Fact>]
    member __.``Searching with columns specified as 'topic' & 'surname' will return just one column``() = 
        let result = 
            __.Query("i1 eq '1'")
            |> __.AddColumns([| "et1"; "t1" |])
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(2, result.Documents.[0].Fields.Count)
        Assert.True(result.Documents.[0].Fields.ContainsKey("et1"))
        Assert.True(result.Documents.[0].Fields.ContainsKey("t1"))
    
    [<Fact>]
    member __.``SearchAsDictionarySeq will return the id column populated in Fields``() = 
        let (result, _, _) = 
            __.Query("i1 eq '1'")
            |> __.AddColumns([| "et1"; "t1" |])
            |> __.SearchFlatResults
            |> __.ExpectSuccess
        Assert.True(result.ToList().[0].ContainsKey(Constants.IdField))
    
    [<Fact>]
    member __.``SearchAsDictionarySeq will return the lastmodified column populated in Fields``() = 
        let (result, _, _) = 
            __.Query("i1 eq '1'")
            |> __.SearchFlatResults
            |> __.ExpectSuccess
        Assert.True(result.ToList().[0].ContainsKey(Constants.LastModifiedField))
    
    [<Fact>]
    member __.``SearchAsDictionarySeq will return the type column populated in Fields``() = 
        let (result, _, _) = 
            __.Query("i1 eq '1'")
            |> __.SearchFlatResults
            |> __.ExpectSuccess
        Assert.True(result.ToList().[0].ContainsKey(Constants.TypeField))
    
    [<Fact>]
    member __.``SearchAsDictionarySeq will return the _score column populated in Fields``() = 
        let (result, _, _) = 
            __.Query("i1 eq '1'")
            |> __.SearchFlatResults
            |> __.ExpectSuccess
        Assert.True(result.ToList().[0].ContainsKey("_score"))
    
    [<Fact>]
    member __.``No score will be returned if ReturnScore is set to false``() = 
        let result = 
            __.Query("i1 eq '1'")
            |> __.WithNoScore
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<double>(0.0, result.Documents.[0].Score)
    
    [<Fact>]
    member __.``Stored field cannot be searched``() = 
        let result = 
            __.Query("s1 eq '1'")
            |> __.SearchResults
            |> __.ExpectFailure
        result |> __.ExpectErrorCode Errors.STORED_FIELDS_CANNOT_BE_SEARCHED
    
    [<Fact>]
    member __.``Stored fields can be retrieved``() = 
        let result = 
            __.Query("i1 eq '1'")
            |> __.AddColumns([| "s1" |])
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.True(result.Documents.[0].Fields.ContainsKey("s1"))

type ``Paging Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,t1,t2,i1
1,Aaron,jhonson,1
2,aron,hewitt,1
3,Airon,Garner,1
4,aroon,Garner,1
5,aronn,jhonson,1
6,aroonn,jhonson,1"""
    do self.TestData <- testData
    
    [<Fact>]
    member __.``Searching for 'i1 = 1' with Count = 2 will return 2 records``() = 
        let result = 
            __.Query("i1 eq '1'")
            |> __.WithCount(2)
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(2, result.Documents.Count)
        Assert.Equal<string>("1", result.Documents.[0].Id)
        Assert.Equal<string>("2", result.Documents.[1].Id)
    
    [<Theory>]
    [<InlineAutoMockIntegrationDataAttribute(1, "2", "3")>]
    [<InlineAutoMockIntegrationDataAttribute(2, "3", "4")>]
    [<InlineAutoMockIntegrationDataAttribute(3, "4", "5")>]
    [<InlineAutoMockIntegrationDataAttribute(4, "5", "6")>]
    member __.``Searching for 'i1 = 1' with records to return = 2 and skip = x will return 2 records`` (skip : int, 
                                                                                                        expected1 : string, 
                                                                                                        expected2 : string) = 
        let result = 
            __.Query("i1 eq '1'")
            |> __.WithCount(2)
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(2, result.Documents.Count)
        Assert.Equal<string>("1", result.Documents.[0].Id)
        Assert.Equal<string>("2", result.Documents.[1].Id)

type ``Sorting Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,et1,t2,i1
1,a,jhonson,1
2,c,hewitt,1
3,b,Garner,1
4,e,Garner,1
5,d,jhonson,1"""
    do self.TestData <- testData
    [<Fact>]
    member __.``Searching for 'i1 = 1' with orderby et1 should return 5 records``() = 
        let result = 
            __.Query("i1 eq '1'")
            |> __.AddColumns([| "et1" |])
            |> __.OrderBy("et1")
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(5, result.Documents.Count)
        Assert.Equal<string>("a", result.Documents.[0].Fields.["et1"])
        Assert.Equal<string>("b", result.Documents.[1].Fields.["et1"])
        Assert.Equal<string>("c", result.Documents.[2].Fields.["et1"])
        Assert.Equal<string>("d", result.Documents.[3].Fields.["et1"])
        Assert.Equal<string>("e", result.Documents.[4].Fields.["et1"])

type ``Highlighting Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,et1,h1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
"""
    do self.TestData <- testData
    [<Fact>]
    member __.``Searching for abstract match 'practical approach' with orderby topic should return 1 records``() = 
        let hlighlightOptions = new HighlightOption(new List<string>([ "h1" ]))
        hlighlightOptions.FragmentsToReturn <- 1
        hlighlightOptions.PreTag <- "<imp>"
        hlighlightOptions.PostTag <- "</imp>"
        let result = 
            __.Query("h1 match 'practical approach'")
            |> __.AddColumns([| "*" |])
            |> __.WithSearchHighlighting hlighlightOptions
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(1, result.Documents.Count)
        Assert.Equal<int>(1, result.Documents.[0].Highlights.Count)
        Assert.True
            (result.Documents.[0].Highlights.[0].Contains("practical"), 
             "The highlighted passage should contain 'practical'")
        Assert.True
            (result.Documents.[0].Highlights.[0].Contains("approach"), 
             "The highlighted passage should contain 'approach'")
        Assert.True
            (result.Documents.[0].Highlights.[0].Contains("<imp>practical</imp>"), 
             "The highlighted should contain 'practical' with in pre and post tags")
        Assert.True
            (result.Documents.[0].Highlights.[0].Contains("<imp>approach</imp>"), 
             "The highlighted should contain 'approach' with in pre and post tags")

type ``Search profile Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,et1,t2,i1,t1
1,a,jhonson,1,aron
2,c,hewitt,1,jhon
3,c,hewitt,1,jhon
4,d,hewitt,1,jhon
5,d,hewitt,1,jhon
6,b,Garner,1,joe
7,e,Garner,1,sam
8,d,jhonson,1,andrew"""
    do self.TestData <- testData
    
    [<Fact>]
    member __.``There are 8 records in the index``() = 
        let result = 
            __.Query("_id matchall '*'")
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(8, result.Documents.Count)
            
    [<Fact>]
    member __.``Searching with searchprofile 'profile1' will return 2 record``() = 
        let result = 
            __.Query("t1:'jhon',t2:'hewitt',i1:'1',et1:'c'")
            |> __.WithSearchProfile "profile1"
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(2, result.Documents.Count)
    
    [<Fact>]
    member __.``If no value for i1 is passed then the default configured value of 1 will be used``() = 
        let result = 
            __.Query("t1:'jhon',t2:'hewitt',et1:'c'")
            |> __.WithSearchProfile "profile1"
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(2, result.Documents.Count)
    
    [<Fact>]
    member __.``If no value for i1 is passed and no value for et1 is passed then et1 will be ignored``() = 
        let result = 
            __.Query("t1:'jhon',t2:'hewitt'")
            |> __.WithSearchProfile "profile1"
            |> __.SearchResults
            |> __.ExpectSuccess
        Assert.Equal<int>(4, result.Documents.Count)
    
    [<Fact>]
    member __.``If no value for t1 is passed then the profile will throw error as that option is set``() = 
        let result = 
            __.Query("t2:'hewitt'")
            |> __.WithSearchProfile "profile1"
            |> __.SearchResults
            |> __.ExpectFailure
        result |> __.ExpectErrorCode Errors.MISSING_FIELD_VALUE
    
    [<Fact>]
    member __.``If no value for t2 is passed then the profile will throw error as the value is missing``() = 
        let result = 
            __.Query("t1:'jhon'")
            |> __.WithSearchProfile "profile1"
            |> __.SearchResults
            |> __.ExpectFailure
        result |> __.ExpectErrorCode Errors.MISSING_FIELD_VALUE
