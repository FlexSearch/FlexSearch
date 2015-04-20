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
