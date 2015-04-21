module SearchTests

open FlexSearch.Core
open Swensen.Unquote
open System.Linq

/// General search related helpers
let getQuery (indexName, queryString) = new SearchQuery.Dto(indexName, queryString)

let withColumns (columns : string []) (query : SearchQuery.Dto) = 
    query.Columns <- columns
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
