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
type SearchTestsBase(ih: IntegrationHelper) =
    do ih |> indexData testData
    abstract ``Works with Exact Field`` : unit -> unit
    abstract ``Works with Id Field`` : unit -> unit
    abstract ``Works with TimeStamp Field`` : unit -> unit
    abstract ``Works with ModifyIndex Field`` : unit -> unit
    abstract ``Works with Int Field`` : unit -> unit
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
    abstract ``Filter query`` : unit -> string
    abstract ``Works with AndOr clause`` : unit -> unit
    abstract ``Works with Multiple params`` : unit -> unit
    abstract ``Works with Functions`` : unit -> unit
    abstract ``Works with Constants`` : unit -> unit
    member __.``Works with Filter clause``() = 
        let query = __.``Filter query``()
        let andScore = ih |> getScore query
        let filterScore = ih |> getScore (sprintf "%s and filter(%s)" query query)
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
id,et1,h1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
"""
    do indexTestData (testData, index, indexService, documentService)
    member __.``Searching for abstract match 'practical approach' with orderby topic should return 1 records``() = 
        let hlighlightOptions = new HighlightOption([| "h1" |])
        hlighlightOptions.FragmentsToReturn <- 1
        hlighlightOptions.PreTag <- "<imp>"
        hlighlightOptions.PostTag <- "</imp>"
        let result = 
            getQuery (index.IndexName, "allof(h1, 'practical approach')")
            |> withColumns [| "*" |]
            |> withHighlighting hlighlightOptions
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        test <@ result.Documents.[0].Highlights.Count() = 1 @>
        test <@ result.Documents.[0].Highlights.[0].Contains("practical") @>
        test <@ result.Documents.[0].Highlights.[0].Contains("approach") @>
        test <@ result.Documents.[0].Highlights.[0].Contains("<imp>practical</imp>") @>
        test <@ result.Documents.[0].Highlights.[0].Contains("<imp>approach</imp>") @>

    interface IDisposable with
        member __.Dispose() = test <@ indexService.DeleteIndex index.IndexName |> succeeded @>

type ``Search profile Tests``(index : Index, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,et1,et2,i1,i2
1,a,h,37,95
2,b,g,49,31
3,c,f,61,52
4,d,e,84,2
5,e,d,12,72
6,f,c,60,30
7,g,b,28,15
8,h,a,41,56"""
    do 
        // Add test profiles
        index.PredefinedQueries <- 
            [| 
                getQuery(index.IndexName, "allof(et1, @et1)") |> withName "matchself"
                getQuery(index.IndexName, "allof(et1, @et1)") |> withName "matchselferror"
                getQuery(index.IndexName, "allof(et1, isblank(@et1, @IGNORE))") |> withName "matchselfignore"
                getQuery(index.IndexName, "allof(et1, isblank(@et1, 'd'))") |> withName "matchselfdefault"
                getQuery(index.IndexName, "allof(et1, @et2)") |> withName "crossmatch"
                getQuery(index.IndexName, "allof(et1, @et2)") |> withName "crossmatcherror"
                getQuery(index.IndexName, "allof(et1, isblank(@et2, @IGNORE))") |> withName "crossmatchignore"
                getQuery(index.IndexName, "allof(et1, isblank(@et2, 'd'))") |> withName "crossmatchdefault"
                getQuery(index.IndexName, "allof(et1, 'h')") |> withName "constantmatch"
                getQuery(index.IndexName, "allof(i1, add(@i2,@i1,'-2'))") |> withName "crossmatchwithfunc"
                getQuery(index.IndexName, "allof(i1, add(@i2,add(@i1,'-2')))") |> withName "crossmatchwithnestedfunc"
                getQuery(index.IndexName, "allof(i1, add('10','18'))") |> withName "matchwithfuncconstonly"
                getQuery(index.IndexName, "allof(i1, add(@i2,@i2))") |> withName "crossmatchwithfieldonlyfunc"
                getQuery(index.IndexName, "allof(et1, isblank(@et1, @et2))") |> withName "nestedcrossmatch"
            |]
        indexTestData (testData, index, indexService, documentService)
    
    member __.``When no value is passed in search profile then the field should match against itself``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchself"
            |> withVariables [("et1", "a")]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1

    member __.``When the passed value is blank then the search will fail as the default behaviour is to throw error``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchself"
            |> withVariables [("et1", "")]
            |> searchService.Search
        test <@ result = fail(MissingVariableValue("et1")) @>

    member __.``When the required field value is not passed then the search will fail as the default behaviour is to throw error``() =
        let result = 
            // Can't use blank query string other wise the query validation will fail
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchself"
            |> withVariables [("et2", "")]
            |> searchService.Search
        test <@ result = fail(MissingVariableValue("et1")) @>

    member __.``When the passed value is blank then the search will fail for self match configuration of [!]``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchselferror"
            |> withVariables [("et1", "")]
            |> searchService.Search
        test <@ result = fail(MissingVariableValue("et1")) @>

    member __.``When the required field value is not passed then the search will fail for self match configuration of [!]``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchselferror"
            |> withVariables [("et2", "")]
            |> searchService.Search
        test <@ result = fail(MissingVariableValue("et1")) @>

    member __.``When the passed value is blank then the search will ignore the clause for self match configuration of [*]``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchselfignore"
            |> withVariables [("et1", "")]
            |> searchAndExtract searchService
        // We should get all the records back as the query will be short circuited to match all query
        result |> assertReturnedDocsCount 8

    member __.``When the required field value is not passed then the search will ignore the clause for self match configuration of [*]``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchselfignore"
            |> withVariables [("et2", "")]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 8

    member __.``When the required field value is blank then the search will use the default value for self match configuration of []``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchselfdefault"
            |> withVariables [("et1", "")]
            |> withColumns [| "*" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "et1" "d"

    member __.``When the required field value is not passed then the search will use the default value for self match configuration of []``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchselfdefault"
            |> withVariables [("et2", "")]
            |> withColumns [| "*" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "et1" "d"

    member __.``Cross matching can be done by specifying the target field inside <> brackets``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatch"
            |> withVariables [("et2", "a")]
            |> withColumns [| "*" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "et1" "a"

    member __.``When the cross matched field value is blank then the search will fail as the default behaviour is to throw error``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatch"
            |> withVariables [("et2", "")]
            |> searchService.Search
        test <@ result = fail(MissingVariableValue("et2")) @>

    member __.``When the cross matched field is not passed then the search will fail as the default behaviour is to throw error``() =
        let result = 
            // Can't use blank query string other wise the query validation will fail
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatch"
            |> withVariables [("et1", "")]
            |> searchService.Search
        test <@ result = fail(MissingVariableValue("et2")) @>

    member __.``When the cross matched field value is blank then the search will fail for cross match configuration of [!]``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatcherror"
            |> withVariables [("et2", "")]
            |> searchService.Search
        test <@ result = fail(MissingVariableValue("et2")) @>

    member __.``When the required field value is not passed then the search will fail for cross match configuration of [!]``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatcherror"
            |> withVariables [("et1", "")]
            |> searchService.Search
        test <@ result = fail(MissingVariableValue("et2")) @>

    member __.``When the cross matched field value is blank then the search will ignore the clause for cross match configuration of [*]``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatchignore"
            |> withVariables [("et2", "")]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 8

    member __.``When the required field value is not passed then the search will ignore the clasue for cross match configuration of [*]``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatchignore"
            |> withVariables [("et1", "")]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 8

    member __.``When the cross matched field value is blank then the search will use the default value for cross match configuration of []``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatchdefault"
            |> withVariables [("et2", "")]
            |> withColumns [| "*" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "et1" "d"

    member __.``When the required field value is not passed then the search will use the default value for cross match configuration of []``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatchdefault"
            |> withVariables [("et1", "")]
            |> withColumns [| "*" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "et1" "d"

    member __.``Constant field value matching is possible by passing the constant value between two quotes``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "constantmatch"
            |> withVariables [("any", "any")]
            |> withColumns [| "*" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "et1" "h"

    member __.``Matching with functions is possible by writing functions in javascript fashion``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatchwithfunc"
            |> withVariables [("i1", "40"); ("i2", "23")]
            |> withColumns [| "_id" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "3"

    member __.``Matching with nested functions is possible by making a function parameter a function call``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatchwithnestedfunc"
            |> withVariables [("i1", "40"); ("i2", "23")]
            |> withColumns [| "_id" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "3"

    member __.``Matching with functions having constant only parameters is possible``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "matchwithfuncconstonly"
            |> withVariables [("i1", "40"); ("i2", "23")]
            |> withColumns [| "_id" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "7"

    member __.``Matching with functions having field only parameters is possible``() =
        let result = 
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "crossmatchwithfieldonlyfunc"
            |> withVariables [("i1", "40"); ("i2", "23")]
            |> withColumns [| "_id" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "6"

    member __.``Nested crossmatching is possible by matching a different field if the main one is blank``() =
        let result =
            getQuery (index.IndexName, null)
            |> withPredefinedQuery "nestedcrossmatch"
            |> withVariables [("et2", "b")]
            |> withColumns [| "_id" |]
            |> withPredefinedQueryOverride
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "2"

    interface IDisposable with
        member __.Dispose() = test <@ indexService.DeleteIndex index.IndexName |> succeeded @>

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
// Query type tests
// ----------------------------------------------------------------------------
type ``Phrase Match Tests``(index : Index, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 
    let testData = """
id,et1,t1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artifacts such as machine code of computer programs.
"""
    do indexTestData (testData, index, indexService, documentService)
    
    member __.``Searching for 'practical approach' with a slop of 1 will return 1 result``() = 
        getQuery (index.IndexName, "upto1wordsapart(t1, 'practical approach')")
        |> searchAndExtract searchService
        |> assertReturnedDocsCount 1
    
    member __.``Searching for 'practical approach' with a default slop of 1 will return 1 result``() = 
        getQuery (index.IndexName, "uptowordsapart(t1, 'practical approach')")
        |> searchAndExtract searchService
        |> assertReturnedDocsCount 1
    
    member __.``Searching for 'approach practical' will not return anything as the order matters``() = 
        getQuery (index.IndexName, "uptowordsapart(t1, 'approach practical')")
        |> searchAndExtract searchService
        |> assertReturnedDocsCount 0
    
    member __.``Searching for 'approach computation' with a slop of 2 will return 1 result``() = 
        getQuery (index.IndexName, "upto2wordsapart(t1, 'approach computation')")
        |> searchAndExtract searchService
        |> assertReturnedDocsCount 1
    
    member __.``Searching for 'comprehensive process leads' with a slop of 1 will return 1 result``() = 
        getQuery (index.IndexName, "upto1wordsapart(t1, 'comprehensive process leads')")
        |> searchAndExtract searchService
        |> assertReturnedDocsCount 1

    interface IDisposable with
        member __.Dispose() = test <@ indexService.DeleteIndex index.IndexName |> succeeded @>

type ``Term Match Complex Tests``(ih : IntegrationHelper) = 
    let testData = """
id,et1,t1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artifacts such as machine code of computer programs.
"""
    do ih |> indexData testData
    member __.``Searching for multiple words will create a new query which will search all the words but not in specific order``() = 
        ih |> verifyResultCount 1 "allof(t1, 'CompSci abbreviated approach')"
    member __.``Searching for multiple words will create a new query which will search all the words using AND style construct but not in specific order``() = 
        ih |> verifyResultCount 0 "allof(t1, 'CompSci abbreviated approach undefinedword')"
    member __.``Setting 'clausetype' in condition properties can override the default clause construction from AND style to OR``() = 
        ih |> verifyResultCount 1 "anyof(t1, 'CompSci abbreviated approach undefinedword')"

type ``Fuzzy WildCard Match Tests``(ih : IntegrationHelper) = 
    let testData = """
id,t1,t2,i1
1,Aaron,johnson,23
2,aron,hewitt,32
3,Airon,Garner,44
4,aroon,Garner,43
5,aronn,johnson,332
6,aroonn,johnson,332
7,boat,,johnson,332
8,moat,johnson,332
"""
    do ih |> indexData testData
    member __.``Searching for 't1 = aron' with default slop of 1 should return 5 records``() = 
        ih |> verifyResultCount 5 "fuzzy(t1, 'aron')"
    member __.``Searching for 't1 = aron' with specified slop of 1 should return 5 records``() = 
        ih |> verifyResultCount 5 "fuzzy1(t1, 'aron')"
    member __.``Searching for 't1 = aron' with slop of 2 should return 6 records``() = 
        ih |> verifyResultCount 6 "fuzzy2(t1, 'aron')"
    member __.``Searching for 't1 = aron?' should return 1 records``() = 
        ih |> verifyResultCount 1 "like(t1, 'aron?')"
    member __.``Searching for 't1 = aron*' should return 2 records``() = 
        ih |> verifyResultCount 2 "like(t1, 'aron*')"
    member __.``Searching for 't1 = ar?n' should return 1 records``() = 
        ih |> verifyResultCount 1 "like(t1, 'ar?n')"
    member __.``Searching for 't1 = AR?N' should return 1 records as matching is case in-sensitive even though like bypasses analysis``() = 
        ih |> verifyResultCount 1 "like(t1, 'AR?N')"
    member __.``Searching for 't1 = [mb]oat' should return 2 records``() = 
        ih |> verifyResultCount 2 "regex(t1, '[mb]oat')"

type ``Range Query Tests``(ih : IntegrationHelper) = 
    let testData = """
id,i1
1,1
2,5
3,10
4,15
5,20
"""
    do ih |> indexData testData
    member __.``Searching for records with i1 in range 1 to 20 inclusive upper & lower bound should return 5 records``() = 
        ih |> verifyResultCount 5 "ge(i1, '1') and le(i1, '20')"
    member __.``Searching for records with cvv in range 1 to 20 exclusive upper & lower bound should return 3 records``() = 
        ih |> verifyResultCount 3 "gt(i1, '1') and lt(i1, '20')"
    member __.``Searching for records with cvv in range 1 to 20 inclusive upper & exclusive lower bound should return 4 records``() = 
        ih |> verifyResultCount 4 "ge(i1, '1') and lt(i1, '20')"
    member __.``Searching for records with cvv in range 1 to 20 excluding upper & including lower bound should return 4 records``() = 
        ih |> verifyResultCount 4 "gt(i1 , '1') and le(i1 , '20')"
    member __.``Searching for records with i1 > '1' should return 4"``() = 
        ih |> verifyResultCount 4 "gt(i1 , '1')"
    member __.``Searching for records with i1 >= '1' should return 5``() = 
        ih |> verifyResultCount 5 "ge(i1 , '1')"
    member __.``Searching for records with i1 < '20' should return 4``() = 
        ih |> verifyResultCount 4 "lt(i1 , '20')"
    member __.``Searching for records with i1 <= '20' should return 5``() = 
        ih |> verifyResultCount 5 "le(i1 , '20')"

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
    do 
        ih |> indexData testData

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

// ----------------------------------------------------------------------------
// Queries with functions tests
// ----------------------------------------------------------------------------
type ``Queries with functions tests``(index : Index, searchService : ISearchService, indexService : IIndexService, documentService : IDocumentService) = 

    let testData = """
id,et1,b1,i1
1,A,TRUE,54
2,aa,true,76
3,Aa,True,3
4,aA,False,87
CC,AA,FALSE,40
5,fsharp,true,0
"""
    do indexTestData (testData, index, indexService, documentService)

    member __.``Searching for i1 = add('10','30') should return record CC which is equal to 40``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1 , add('10','30'))")
            |> withColumns [| "_id"; "i1" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "CC"

    member __.``Searching for i1 = add( '10', '30' ) using spaces should return record CC which is equal to 40``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, add( '10', '30' ))")
            |> withColumns [| "_id"; "i1" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "CC"

    member __.``Searching for i1 = add('10', add('10','20')) having nested functions should return record CC which is equal to 40``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1, add('10', add('10','20')))")
            |> withColumns [| "_id"; "i1" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "CC"

    member __.``Searching for i1 > add('10','30') should return 3 records``() = 
        let result = 
            getQuery (index.IndexName, "gt(i1 , add('10','30'))")
            |> withColumns [| "_id"; "i1" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 3
        result.Documents |> Seq.forall (fun d -> d.Fields.["i1"] |> Convert.ToInt32 > 40)

    member __.``Searching for i1 = add('80', '-4') should support negative numbers``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1 , add('80', '-4'))")
            |> withColumns [| "_id"; "i1" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "2"

    member __.``Searching for i1 = add('10', '10', '10', '10') should support more than 2 parameters``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1 , add('10', '10', '10', '10'))")
            |> withColumns [| "_id"; "i1" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "CC"

    member __.``Searching for i1 = add('3') should support only one parameter``() = 
        let result = 
            getQuery (index.IndexName, "allof(i1 , add('3'))")
            |> withColumns [| "_id"; "i1" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "3"

    member __.``Function-only conditions shouldn't be allowed for all functions``() =
        let result =
            getQuery (index.IndexName, "upper(et1)")
            |> withColumns [| "_id" |]
            |> searchService.Search
        
        test <@ result = (fail <| RhsValueNotFound("upper")) @>

    member __.``endswith should be allowed in function-only conditions``() =
        let result = 
            getQuery (index.IndexName, "endswith(et1, 'sharp')")
            |> withColumns [| "_id" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "_id" "5"

    member __.``startsswith should be allowed in function-only conditions``() =
        let result = 
            getQuery (index.IndexName, "startswith(et1, 'fs')")
            |> withColumns [| "et1" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 1
        result |> assertFieldValue 0 "et1" "fsharp"

    interface IDisposable with
        member __.Dispose() = test <@ indexService.DeleteIndex index.IndexName |> succeeded @>