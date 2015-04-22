module SearchTests

open FlexSearch.Core
open Swensen.Unquote
open System.Linq

/// General search related helpers
let getQuery (indexName, queryString) = new SearchQuery.Dto(indexName, queryString)

let withColumns (columns : string []) (query : SearchQuery.Dto) = 
    query.Columns <- columns
    query

let withSearchProfile (profileName : string) (query : SearchQuery.Dto) = 
    query.SearchProfile <- profileName
    query

let withHighlighting (option : HighlightOption.Dto) (query : SearchQuery.Dto) = 
    query.Highlights <- option
    query

let withOrderBy (column : string) (query : SearchQuery.Dto) = 
    query.OrderBy <- column
    query

let withNoScore (query : SearchQuery.Dto) = 
    query.ReturnScore <- false
    query

let withCount (count : int) (query : SearchQuery.Dto) = 
    query.Count <- count
    query

let withSkip (skip : int) (query : SearchQuery.Dto) = 
    query.Skip <- skip
    query

let searchAndExtract (searchService : ISearchService) (query) = 
    let result = searchService.Search(query)
    test <@ succeeded <| result @>
    extract <| result

let searchForFlatAndExtract (searchService : ISearchService) (query) = 
    let result = searchService.SearchAsDictionarySeq(query)
    test <@ succeeded <| result @>
    let (docs, _, _) = extract <| result
    docs.ToList()

/// Assertions
/// Checks if the total number of fields returned by the query matched the expected
/// count
let assertFieldCount (expected : int) (result : SearchResults) = test <@ result.Documents.[0].Fields.Count = expected @>

/// Check if the total number of document returned by the query matched the expected
/// count
let assertReturnedDocsCount (expected : int) (result : SearchResults) = 
    test <@ result.Documents.Count = expected @>
    test <@ result.RecordsReturned = expected @>

let assertFieldValue (documentNo : int) (fieldName : string) (expectedFieldValue : string) (result : SearchResults) = 
    test <@ result.Documents.Count >= documentNo @>
    test <@ result.Documents.[documentNo].Fields.[fieldName] = expectedFieldValue @>

/// Check if the total number of available document returned by the query matched the expected
/// count
let assertAvailableDocsCount (expected : int) (result : SearchResults) = test <@ result.TotalAvailable = expected @>

/// Check if the field is present in the document returned by the query
let assertFieldPresent (expected : string) (result : SearchResults) = 
    test <@ result.Documents.[0].Fields.ContainsKey(expected) @>

type ``Column Tests``(index : Index.Dto, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,et1,t1,i1,s1
1,a,jhonson,1,test1
2,c,hewitt,1,test2
3,b,Garner,1,test3
4,e,Garner,1,test4
5,d,jhonson,1,test5"""
    do indexTestData (testData, index, indexService, documentService)
    
    member __.``Searching with no columns specified will return no additional columns``() = 
        let result = getQuery (index.IndexName, "i1 eq '1'") |> searchAndExtract searchService
        result |> assertFieldCount 0
    
    member __.``Searching with columns specified with '*' will return all column``() = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withColumns [| "*" |]
            |> searchAndExtract searchService
        result |> assertFieldCount index.Fields.Length
    
    member __.``Searching with columns specified as 'topic' will return just one column``() = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withColumns [| "et1" |]
            |> searchAndExtract searchService
        result |> assertFieldCount 1
        result |> assertFieldPresent "et1"
    
    member __.``Searching with columns specified as 'topic' & 'surname' will return just one column``() = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withColumns [| "et1"; "t1" |]
            |> searchAndExtract searchService
        result |> assertFieldCount 2
        result |> assertFieldPresent "et1"
        result |> assertFieldPresent "t1"
    
    member __.``SearchAsDictionarySeq will return the id column populated in Fields``() = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withColumns [| "et1"; "t1" |]
            |> searchForFlatAndExtract searchService
        test <@ result.[0].ContainsKey(Constants.IdField) @>
    
    member __.``SearchAsDictionarySeq will return the lastmodified column populated in Fields``() = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withColumns [| "et1"; "t1" |]
            |> searchForFlatAndExtract searchService
        test <@ result.[0].ContainsKey(Constants.LastModifiedField) @>
    
    //    member __.``SearchAsDictionarySeq will return the type column populated in Fields``() = 
    //        let result = 
    //            getQuery (index.IndexName, "i1 eq '1'")
    //            |> withColumns [| "et1"; "t1" |]
    //            |> searchForFlatAndExtract searchService
    //        test <@ result.[0].ContainsKey(Constants.Type) @>
    member __.``SearchAsDictionarySeq will return the _score column populated in Fields``() = 
        let result = getQuery (index.IndexName, "i1 eq '1'") |> searchForFlatAndExtract searchService
        test <@ result.[0].ContainsKey(Constants.Score) @>
    
    member __.``No score will be returned if ReturnScore is set to false``() = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withNoScore
            |> searchAndExtract searchService
        test <@ result.Documents.[0].Score = 0.0 @>
    
    member __.``Stored field cannot be searched``() = 
        let query = getQuery (index.IndexName, "s1 eq '1'")
        test <@ searchService.Search(query) = Choice2Of2(StoredFieldCannotBeSearched("s1")) @>
    
    member __.``Stored fields can be retrieved``() = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withColumns [| "s1" |]
            |> searchAndExtract searchService
        result |> assertFieldPresent "s1"

type ``Paging Tests``(index : Index.Dto, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,t1,t2,i1
1,Aaron,jhonson,1
2,aron,hewitt,1
3,Airon,Garner,1
4,aroon,Garner,1
5,aronn,jhonson,1
6,aroonn,jhonson,1"""
    do indexTestData (testData, index, indexService, documentService)
    
    member __.``Searching for 'i1 = 1' with Count = 2 will return 2 records``() = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withCount 2
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 2
        test <@ result.Documents.[0].Id = "1" @>
        test <@ result.Documents.[1].Id = "2" @>
    
    [<InlineData(1, "2", "3")>]
    [<InlineData(2, "3", "4")>]
    [<InlineData(3, "4", "5")>]
    [<InlineData(4, "5", "6")>]
    member __.``Searching for 'i1 = 1' with records to return = 2 and skip = x will return 2 records`` (skip : int, 
                                                                                                        expected1 : string, 
                                                                                                        expected2 : string) = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withCount 2
            |> withSkip skip
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 2
        test <@ result.Documents.[0].Id = expected1 @>
        test <@ result.Documents.[1].Id = expected2 @>

type ``Sorting Tests``(index : Index.Dto, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,et1,t2,i1
1,a,jhonson,1
2,c,hewitt,1
3,b,Garner,1
4,e,Garner,1
5,d,jhonson,1"""
    do indexTestData (testData, index, indexService, documentService)
    member __.``Searching for 'i1 = 1' with orderby et1 should return 5 records``() = 
        let result = 
            getQuery (index.IndexName, "i1 eq '1'")
            |> withColumns [| "et1" |]
            |> withOrderBy "et1"
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 5
        result |> assertFieldValue 0 "et1" "a"
        result |> assertFieldValue 1 "et1" "b"
        result |> assertFieldValue 2 "et1" "c"
        result |> assertFieldValue 3 "et1" "d"
        result |> assertFieldValue 4 "et1" "e"

type ``Highlighting Tests``(index : Index.Dto, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,et1,h1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
"""
    do indexTestData (testData, index, indexService, documentService)
    member __.``Searching for abstract match 'practical approach' with orderby topic should return 1 records``() = 
        let hlighlightOptions = new HighlightOption.Dto([| "h1" |])
        hlighlightOptions.FragmentsToReturn <- 1
        hlighlightOptions.PreTag <- "<imp>"
        hlighlightOptions.PostTag <- "</imp>"
        let result = 
            getQuery (index.IndexName, "h1 match 'practical approach'")
            |> withColumns [| "*" |]
            |> withHighlighting hlighlightOptions
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        test <@ result.Documents.[0].Highlights.Count() = 1 @>
        test <@ result.Documents.[0].Highlights.[0].Contains("practical") @>
        test <@ result.Documents.[0].Highlights.[0].Contains("approach") @>
        test <@ result.Documents.[0].Highlights.[0].Contains("<imp>practical</imp>") @>
        test <@ result.Documents.[0].Highlights.[0].Contains("<imp>approach</imp>") @>

type ``Search profile Tests``(index : Index.Dto, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
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
    do indexTestData (testData, index, indexService, documentService)
    
    member __.``There are 8 records in the index``() = 
        let result = getQuery (index.IndexName, "_id matchall '*'") |> searchAndExtract searchService
        result |> assertReturnedDocsCount 8
    
    member __.``Searching with searchprofile 'profile1' will return 2 record``() = 
        let result = 
            getQuery (index.IndexName, "t1:'jhon',t2:'hewitt',i1:'1',et1:'c'")
            |> withSearchProfile "profile1"
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 2
    
    member __.``If no value for i1 is passed then the default configured value of 1 will be used``() = 
        let result = 
            getQuery (index.IndexName, "t1:'jhon',t2:'hewitt',et1:'c'")
            |> withSearchProfile "profile1"
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 2
    
    member __.``If no value for i1 is passed and no value for et1 is passed then et1 will be ignored``() = 
        let result = 
            getQuery (index.IndexName, "t1:'jhon',t2:'hewitt'")
            |> withSearchProfile "profile1"
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 4
    
    member __.``If no value for t1 is passed then the profile will throw error as that option is set``() = 
        let result = 
            getQuery (index.IndexName, "t2:'hewitt'")
            |> withSearchProfile "profile1"
            |> searchService.Search
        test <@ result = Choice2Of2(MissingFieldValue("t1")) @>
    
    member __.``If no value for t2 is passed then the profile will throw error as the value is missing``() = 
        let result = 
            getQuery (index.IndexName, "t1:'jhon'")
            |> withSearchProfile "profile1"
            |> searchService.Search
        test <@ result = Choice2Of2(MissingFieldValue("t2")) @>
