namespace FlexSearch.Tests.SearchTests

open FlexSearch.Tests
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open FlexSearch.Core
open Swensen.Unquote
open System.Linq
open System

/// Defines the minimum set of tests that should be implemented for a 
/// new operator
[<AbstractClass>]
type SearchTestsBase(ih : IntegrationHelper) = 
    do ih |> indexData testData
    abstract ``Works with Exact Field`` : unit -> unit
    abstract ``Works with Id Field`` : unit -> unit
    abstract ``Works with TimeStamp Field`` : unit -> unit
    abstract ``Works with ModifyIndex Field`` : unit -> unit
    abstract ``Works with Int Field`` : unit -> unit
    abstract ``Works with Multiple Int input`` : unit -> unit
    abstract ``Works with Long Field`` : unit -> unit
    abstract ``Works with Double Field`` : unit -> unit
    abstract ``Works with Float Field`` : unit -> unit
    abstract ``Works with DateTime Field`` : unit -> unit
    abstract ``Works with Date Field`` : unit -> unit
    abstract ``Works with Bool Field`` : unit -> unit
    abstract ``Works with Stored Field`` : unit -> unit
    abstract ``Works with And clause`` : unit -> unit
    abstract ``Works with Or clause`` : unit -> unit
    abstract ``Works with Not clause`` : unit -> unit
    
    [<Ignore>]
    abstract ``Filter query`` : unit -> string * string
    
    abstract ``Works with AndOr clause`` : unit -> unit
    abstract ``Works with Multiple params`` : unit -> unit
    abstract ``Works with Constants`` : unit -> unit
    member this.``Works with Filter clause``() = 
        let (query1, query2) = this.``Filter query``()
        let andScore = ih |> getScore query1
        let filterScore = ih |> getScore query2
        andScore =? filterScore

(*
Test implementation to copy around
type OperatorTestsBase(ih : IntegrationHelper) =
    inherit SearchTestsBase(ih)
    override __.``Works with Exact Field``() = ()
    override __.``Works with Id Field``() = ()
    override __.``Works with TimeStamp Field``() = ()
    override __.``Works with ModifyIndex Field``() = ()
    override __.``Works with Int Field``() = ()
    override __.````Works with Multiple Int input``() = ()
    override __.``Works with Long Field``() = ()
    override __.``Works with Double Field``() = ()
    override __.``Works with Float Field``() = ()
    override __.``Works with DateTime Field``() = ()
    override __.``Works with Date Field``() = ()
    override __.``Works with Bool Field``() = ()
    override __.``Works with Stored Field``() = ()
    override __.``Works with And clause``() = ()
    override __.``Works with Or clause``() = ()
    override __.``Works with Not clause``() = ()
    override __.``Works with Filter clause``() = ()
    override __.``Works with AndOr clause``() = ()
    override __.``Works with Multiple params``() = ()
    override __.``Works with Functions``() = ()
    override __.``Works with Constants``() = ()
*)

type ``Column Tests``(index : Index, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,et1,t1,i1,s1
1,a,johnson,1,test1
2,c,hewitt,1,test2
3,b,Garner,1,test3
4,e,Garner,1,test4
5,d,johnson,1,test5"""
    do indexTestData (testData, index, indexService, documentService)
    
    member __.``Searching with no columns specified will return no additional columns``() = 
        let result = getQuery (index.IndexName, "allof(i1, '1')") |> searchAndExtract searchService
        result |> assertFieldCount 0
    
    member __.``Searching with columns specified with '*' will return all column``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "*" |]
            |> searchAndExtract searchService
        result |> assertFieldCount index.Fields.Length
    
    member __.``Searching with columns specified as 'topic' will return just one column``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "et1" |]
            |> searchAndExtract searchService
        result |> assertFieldCount 1
        result |> assertFieldPresent "et1"
    
    member __.``Searching with columns specified as 'topic' & 'surname' will return just one column``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "et1"; "t1" |]
            |> searchAndExtract searchService
        result |> assertFieldCount 2
        result |> assertFieldPresent "et1"
        result |> assertFieldPresent "t1"
    
    member __.``Search will return the id column populated as a field of the Document object``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "et1"; "t1" |]
            |> searchExtractDocList searchService
        String.IsNullOrEmpty(result.[0].Id) =? false
    
    member __.``Search will return the lastmodified column as a field of the Document object``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "et1"; "t1" |]
            |> searchExtractDocList searchService
        result.[0].TimeStamp >? 0L
    
    //    member __.``SearchAsDictionarySeq will return the type column populated in Fields``() = 
    //        let result = 
    //            getQuery (index.IndexName, "i1 eq '1'")
    //            |> withColumns [| "et1"; "t1" |]
    //            |> searchForFlatAndExtract searchService
    //        test <@ result.[0].ContainsKey(Constants.Type) @>
    member __.``Search will return the _score column populated as a field of the Documents object``() = 
        let result = getQuery (index.IndexName, "allof(i1 , '1')") |> searchExtractDocList searchService
        result.[0].Score >? 0.0
    
    member __.``No score will be returned if ReturnScore is set to false``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withNoScore
            |> searchAndExtract searchService
        test <@ result.Documents.[0].Score = 0.0 @>
    
    member __.``Stored field cannot be searched``() = 
        let query = getQuery (index.IndexName, "allof(s1, '1')")
        test <@ searchService.Search(query) = fail (StoredFieldCannotBeSearched("s1")) @>
    
    member __.``Stored fields can be retrieved``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "s1" |]
            |> searchAndExtract searchService
        result |> assertFieldPresent "s1"

    interface IDisposable with
        member __.Dispose() = test <@ indexService.DeleteIndex index.IndexName |> succeeded @>

type ``Paging Tests``(index : Index, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,t1,t2,i1
1,Aaron,johnson,1
2,aron,hewitt,1
3,Airon,Garner,1
4,aroon,Garner,1
5,aronn,johnson,1
6,aroonn,johnson,1"""
    do indexTestData (testData, index, indexService, documentService)
    
    member __.``Searching for 'i1 = 1' with Count = 2 will return 2 records``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withCount 2
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 2
        test <@ result.Documents.[0].Id = "1" @>
        test <@ result.Documents.[1].Id = "2" @>
    
    [<InlineData(1, "2", "3")>]
    [<InlineData(2, "3", "4")>]
    [<InlineData(3, "4", "5")>]
    [<InlineData(4, "5", "6")>]
    [<Ignore>]
    member __.``Searching for 'i1 = 1' with records to return = 2 and skip = x will return 2 records`` (skip : int, 
                                                                                                        expected1 : string, 
                                                                                                        expected2 : string) = 
        let result = 
            getQuery (index.IndexName, "allof(i1 ,'1')")
            |> withCount 2
            |> withSkip skip
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 2
        test <@ result.Documents.[0].Id = expected1 @>
        test <@ result.Documents.[1].Id = expected2 @>

    interface IDisposable with
        member __.Dispose() = test <@ indexService.DeleteIndex index.IndexName |> succeeded @>

type ``Sorting Tests``(index : Index, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,et1,t2,i1,i2
1,a,johnson,1,1
2,c,hewitt,1,3
3,b,Garner,1,2
4,e,Garner,1,4
5,d,johnson,1,5"""
    do indexTestData (testData, index, indexService, documentService)

    member __.``Searching for 'i1 = 1' with orderby _lastmodified should return 5 records``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "_id" |]
            |> withOrderBy TimeStampField.Name
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 5
        result |> assertFieldValue 0 "_id" "1"
        result |> assertFieldValue 1 "_id" "2"
        result |> assertFieldValue 2 "_id" "3"
        result |> assertFieldValue 3 "_id" "4"
        result |> assertFieldValue 4 "_id" "5"

    member __.``Searching for 'i1 = 1' with orderby _lastmodified and direction desc should return 5 records``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "_id" |]
            |> withOrderByDesc TimeStampField.Name
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 5
        test <@ result.Documents.[0].TimeStamp >= result.Documents.[1].TimeStamp @>
        test <@ result.Documents.[1].TimeStamp >= result.Documents.[2].TimeStamp @>
        test <@ result.Documents.[2].TimeStamp >= result.Documents.[3].TimeStamp @>
        test <@ result.Documents.[3].TimeStamp >= result.Documents.[4].TimeStamp @>
    
    member __.``Searching for 'i1 = 1' with orderby et1 should return 5 records``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "et1" |]
            |> withOrderBy "et1"
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 5
        result |> assertFieldValue 0 "et1" "a"
        result |> assertFieldValue 1 "et1" "b"
        result |> assertFieldValue 2 "et1" "c"
        result |> assertFieldValue 3 "et1" "d"
        result |> assertFieldValue 4 "et1" "e"

    member __.``Searching for 'i1 = 1' with orderby et1 and direction desc should return 5 records``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "et1" |]
            |> withOrderByDesc "et1"
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 5
        result |> assertFieldValue 0 "et1" "e"
        result |> assertFieldValue 1 "et1" "d"
        result |> assertFieldValue 2 "et1" "c"
        result |> assertFieldValue 3 "et1" "b"
        result |> assertFieldValue 4 "et1" "a"

    member __.``Sorting is possible on int field``() =
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "i2" |]
            |> withOrderBy "i2"
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 5
        result |> assertFieldValue 0 "i2" "1"
        result |> assertFieldValue 1 "i2" "2"
        result |> assertFieldValue 2 "i2" "3"
        result |> assertFieldValue 3 "i2" "4"
        result |> assertFieldValue 4 "i2" "5"

    member __.``Sorting in descending order is possible on int field``() =
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "i2" |]
            |> withOrderByDesc "i2"
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 5
        result |> assertFieldValue 0 "i2" "5"
        result |> assertFieldValue 1 "i2" "4"
        result |> assertFieldValue 2 "i2" "3"
        result |> assertFieldValue 3 "i2" "2"
        result |> assertFieldValue 4 "i2" "1"

    member __.``Sorting is not possible on text field``() =
        let result = 
            getQuery (index.IndexName, "allof(i1, '1')")
            |> withColumns [| "i2" |]
            |> withOrderBy "t2"
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 5
        result |> assertFieldValue 0 "i2" "1"
        result |> assertFieldValue 1 "i2" "3"
        result |> assertFieldValue 2 "i2" "2"
        result |> assertFieldValue 3 "i2" "4"
        result |> assertFieldValue 4 "i2" "5"

    interface IDisposable with
        member __.Dispose() = test <@ indexService.DeleteIndex index.IndexName |> succeeded @>

type ``Highlighting Tests``(index : Index, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,et1,t1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
"""
    do indexTestData (testData, index, indexService, documentService)
    member __.``Searching for abstract match 'practical approach' with orderby topic should return 1 records``() = 
        let hlighlightOptions = new HighlightOption([| "t1" |])
        hlighlightOptions.FragmentsToReturn <- 1
        hlighlightOptions.PreTag <- "<imp>"
        hlighlightOptions.PostTag <- "</imp>"
        let result = 
            getQuery (index.IndexName, "allof(t1, 'practical approach')")
            |> withColumns [| "*" |]
            |> withHighlighting hlighlightOptions
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        test <@ result.Documents.[0].Highlights.Count() = 1 @>
        test <@ result.Documents.[0].Highlights.[0].Contains("practical") @>
        test <@ result.Documents.[0].Highlights.[0].Contains("approach") @>
        test <@ result.Documents.[0].Highlights.[0].Contains("<imp>practical</imp>") @>
        test <@ result.Documents.[0].Highlights.[0].Contains("<imp>approach</imp>") @>

type ``Variable Tests``(ih : IntegrationHelper) = 
    let indexName = ih.Index.IndexName
    let testData = """
id,et1,et2,i1,i2
1,null,h,37,95
2,b,g,49,31
3,c,f,61,52
4,d,e,84,2
5,e,d,12,72
6,f,c,60,30
7,g,b,28,15
8,h,a,41,56"""
    
    do 
        // Add test profiles
        ih.Index.PredefinedQueries <- [| getQuery (indexName, "allof(et1, @et1)") |> withName "variableExists" |]
        ih |> indexData testData
    
    member __.``Predefined query can be used with variables``() = 
        getQuery (indexName, "")
        |> withPredefinedQuery "variableExists"
        |> withVariables [ ("et1", "d") ]
        |> searchAndExtract ih.SearchService
        |> assertReturnedDocsCount 1

    member __.``Query will use the passed in value of a variable``() = 
        getQuery (indexName, "allof(et1, @et1)")
        |> withVariables [ ("et1", "d") ]
        |> searchAndExtract ih.SearchService
        |> assertReturnedDocsCount 1

    member __.``When the passed value of a variable is blank then the search will fail as the default behaviour is to throw error``() = 
        let result = 
            getQuery (indexName, "allof(et1, @et1)")
            |> withVariables [ ("et1", "") ]
            |> ih.SearchService.Search
        test <@ result = fail (MissingVariableValue("et1")) @>

    member __.``When the required variable is not passed then the search will fail as the default behaviour is to throw error``() = 
        let result = getQuery (indexName, "allof(et1, @et1)") |> ih.SearchService.Search
        test <@ result = fail (MissingVariableValue("et1")) @>

    member __.``When matchall switch is used then the search clause will be replaced with match all query``() = 
        getQuery (indexName, "allof(et1, @et1, -matchall)")
        |> searchAndExtract ih.SearchService
        |> assertReturnedDocsCount 8

    member __.``When matchnone switch is used then the search clause will be replaced with match none query``() = 
        getQuery (indexName, "allof(et1, @et1, -matchnone)")
        |> searchAndExtract ih.SearchService
        |> assertReturnedDocsCount 0

    member __.``When usedefault 'value' switch is used then the search clause will be replaced with the default value``() = 
        getQuery (indexName, "allof(et1, @et1, -usedefault 'd')")
        |> searchAndExtract ih.SearchService
        |> assertReturnedDocsCount 1

    member __.``When usedefault switch is used then the search clause will be replaced with the default value``() = 
        getQuery (indexName, "allof(et1, @et1, -usedefault)")
        |> searchAndExtract ih.SearchService
        |> assertReturnedDocsCount 1

    member __.``Variable names should be case insensitive``() =
        getQuery (indexName, "allof(et1, @caseInsensitiveVARIABLE)")
        |> withVariables [ ("caseInSENSITIVEvariable", "d") ]
        |> searchAndExtract ih.SearchService
        |> assertReturnedDocsCount 1

type ``DistinctBy Tests``(index : Index, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,et1,t1,i1,s1
1,a,johnson,1,test1
2,a,hewitt,1,test2
3,b,Garner,1,test3
4,b,Garner,1,test4
5,c,johnson,1,test5"""
    do indexTestData (testData, index, indexService, documentService)
    
    member __.``Setting distinctby removes duplicates from the serach results``() = 
        let result = 
            getQuery (index.IndexName, "matchall(_id, '*')")
            |> withDistinctBy "et1"
            |> searchAndExtract searchService
        // The document returned count will still be 5 as we are filtering records
        test <@ result.Documents.Length = 3 @>
        test <@ result.RecordsReturned = 5 @>

    member __.``Distinctby only works with ExactText fields``() = 
        let result = 
            getQuery (index.IndexName, "matchall(_id, '*')")
            |> withDistinctBy "t1"
            |> searchAndExtract searchService
        test <@ result.Documents.Length = 5 @>
        test <@ result.RecordsReturned = 5 @>

    interface IDisposable with
        member __.Dispose() = test <@ indexService.DeleteIndex index.IndexName |> succeeded @>

// ----------------------------------------------------------------------------
// Analyzer specific search tests
// ----------------------------------------------------------------------------
type ``Phonetic matching tests``(ih : IntegrationHelper) = 
    let testData = """
id,t1
1,smith
2,smyth
3,Schmidt
4,fish
5,phish
"""
    
    do 
        ih.Index.Fields.First(fun x -> x.FieldName = "t1").SearchAnalyzer <- "refinedsoundex"
        ih.Index.Fields.First(fun x -> x.FieldName = "t1").IndexAnalyzer <- "refinedsoundex"
        ih |> indexData testData

    member __.``Searching for t1 = 'smith' should return 2 records as smith and smyth are phonetic equivalents``() = 
        ih |> verifyResultCount 2 "allof(t1, 'smith')"
    member __.``Searching for t1 = 'fish' should return 1 record as fish and phish are not phonetic equivalents``() = 
        ih |> verifyResultCount 1 "allof(t1, 'fish')"

// ----------------------------------------------------------------------------
// Filed type specific search tests
// ----------------------------------------------------------------------------
type ``Exact Matching Field tests``(ih : IntegrationHelper) = 
    let testData = """
id,et1,b1
1,A,TRUE
2,aa,true
3,Aa,True
4,aA,False
CC,AA,FALSE
"""
    do ih |> indexData testData
    member __.``Searching for et1 = 'a' should return 1 records as field is case insensitive``() = 
        ih |> verifyResultCount 1 "allof(et1, 'a')"
    member __.``Searching for et1 = 'aa' should return 4 records as field is case insensitive``() = 
        ih |> verifyResultCount 4 "allof(et1 , 'aa')"
    member __.``Searching for b1 = 'true' should return 3 records as field is case insensitive``() = 
        ih |> verifyResultCount 3 "allof(b1 , 'true')"
    member __.``Searching for b1 = 'FALSE' should return 2 records as field is case insensitive``() = 
        ih |> verifyResultCount 2 "allof(b1 , 'FALSE')"
    member __.``Searching for _id = 'CC' should return 1 records as field is case insensitive``() = 
        ih |> verifyResultCount 1 "allof(_id, 'CC')"
    member __.``Searching for _id = 'cc' should return 1 records as field is case insensitive``() = 
        ih |> verifyResultCount 1 "allof(_id, 'cc')"
