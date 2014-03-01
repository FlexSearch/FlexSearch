// ----------------------------------------------------------------------------
// Flexsearch predefined tokenizers (Tokenizers.fs)
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.Linq
open java.io
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.standard
open org.apache.lucene.analysis.tokenattributes
open org.apache.lucene.analysis.util
open org.apache.lucene.index
open org.apache.lucene.queries
open org.apache.lucene.queryparser.classic
open org.apache.lucene.queryparser.flexible
open org.apache.lucene.search
open org.apache.lucene.search.highlight
open org.apache.lucene.search.postingshighlight

// ----------------------------------------------------------------------------
// Contains all predefined flex queries. Also contains the search factory service.
// The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
[<AutoOpen>]
module SearchDsl = 
    let FlexCharTermAttribute = 
        lazy java.lang.Class.forName ("org.apache.lucene.analysis.tokenattributes.CharTermAttribute")
    
    /// Utility function to get tokens from the search string based upon the passed analyzer
    /// This will enable us to avoid using the lucene query parser
    /// We cannot use simple white space based token generation as it really depends 
    /// upon the analyzer used
    let inline ParseTextUsingAnalyzer(analyzer : Analyzer, fieldName, queryText) = 
        let tokens = new List<string>()
        let source : TokenStream = analyzer.tokenStream (fieldName, new StringReader(queryText))
        // Get the CharTermAttribute from the TokenStream
        let termAtt = source.addAttribute (FlexCharTermAttribute.Force())
        try 
            try 
                source.reset()
                while source.incrementToken() do
                    tokens.Add(termAtt.ToString())
                source.``end``()
            with ex -> ()
        finally
            source.close()
        tokens
    
    let GetQueryModules(factoryCollection : IFactoryCollection) = 
        let queries = factoryCollection.SearchQueryFactory.GetAllModules()
        let result = new Dictionary<string, IFlexQuery>(StringComparer.OrdinalIgnoreCase)
        for query in queries do
            for name in query.Value.QueryName() do
                result.Add(name, query.Value)
        result
    
    let private GenerateQuery (flexIndex : FlexIndex) (predicate : Predicate) (searchQuery : SearchQuery) 
        (isProfileBased : Dictionary<string, string> option) (queryTypes : Dictionary<string, IFlexQuery>) = 
        let rec generateQuery (pred : Predicate) = 
            maybe { 
                let generateMatchAllQuery = ref false
                match pred with
                | NotPredicate(pr) -> 
                    let! notQuery = generateQuery (pr)
                    let query = new BooleanQuery()
                    query.add (new BooleanClause(notQuery, BooleanClause.Occur.MUST_NOT))
                    return (query :> Query)
                | Condition(f, o, v, b) -> 
                    let! fieldType = getValue flexIndex.IndexSetting.FieldsLookup f MessageConstants.INVALID_FIELD_NAME
                    let! query = getValue queryTypes o MessageConstants.INVALID_QUERY_TYPE
                    let! value = maybe { 
                                     match isProfileBased with
                                     | Some(source) -> 
                                         match source.TryGetValue(f) with
                                         | true, v' -> return [| v' |]
                                         | _ -> 
                                             match searchQuery.MissingValueCofiguration.TryGetValue(f) with
                                             | true, configuration -> 
                                                 match configuration with
                                                 | MissingValueOption.Default -> return! v.GetValueAsArray()
                                                 | MissingValueOption.ThrowError -> 
                                                     return! Choice2Of2
                                                                 (OperationMessage.WithPropertyName
                                                                      (MessageConstants.MISSING_FIELD_VALUE_1, f))
                                                 | MissingValueOption.Ignore -> 
                                                     generateMatchAllQuery := true
                                                     return [| "" |]
                                                 | _ -> 
                                                     return! Choice2Of2
                                                                 (OperationMessage.WithPropertyName
                                                                      (MessageConstants.UNKNOWN_MISSING_VALUE_OPTION, f))
                                             | _ -> 
                                                 // Check if a non blank value is provided as a part of the query
                                                 return! v.GetValueAsArray()
                                     | None -> return! v.GetValueAsArray()
                                 }
                    if generateMatchAllQuery.Value = true then return! Choice1Of2(new MatchAllDocsQuery() :> Query)
                    else 
                        let! q = query.GetQuery(fieldType, value.ToArray())
                        match b with
                        | Some(b') -> q.setBoost (float32 (b'))
                        | None -> ()
                        return q
                | OrPredidate(lhs, rhs) -> 
                    let! lhsQuery = generateQuery (lhs)
                    let! rhsQuery = generateQuery (rhs)
                    let query = new BooleanQuery()
                    query.add (new BooleanClause(lhsQuery, BooleanClause.Occur.SHOULD))
                    query.add (new BooleanClause(rhsQuery, BooleanClause.Occur.SHOULD))
                    return query :> Query
                | AndPredidate(lhs, rhs) -> 
                    let! lhsQuery = generateQuery (lhs)
                    let! rhsQuery = generateQuery (rhs)
                    let query = new BooleanQuery()
                    query.add (new BooleanClause(lhsQuery, BooleanClause.Occur.MUST))
                    query.add (new BooleanClause(rhsQuery, BooleanClause.Occur.MUST))
                    return query :> Query
            }
        generateQuery predicate
    
    let private SearchQuery(flexIndex : FlexIndex, query : Query, search : SearchQuery) = 
        let indexSearchers = new List<IndexSearcher>()
        for i in 0..flexIndex.Shards.Length - 1 do
            let searcher = (flexIndex.Shards.[i].NRTManager :> ReferenceManager).acquire() :?> IndexSearcher
            indexSearchers.Add(searcher)
        // Each thread only works on a separate part of the array and as no parts are shared across
        // multiple threads the belows variables are thread safe. The cost of using blockingcollection vs 
        // array per search is high
        let topDocsCollection : TopDocs array = Array.zeroCreate indexSearchers.Count
        
        let sort = 
            match search.OrderBy with
            | null -> Sort.RELEVANCE
            | _ -> 
                match flexIndex.IndexSetting.FieldsLookup.TryGetValue(search.OrderBy) with
                | (true, field) -> new Sort(new SortField(field.FieldName, FlexField.SortField(field)))
                | _ -> Sort.RELEVANCE
        
        let count = 
            match search.Count with
            | 0 -> 10 + search.Skip
            | _ -> search.Count + search.Skip
        
        flexIndex.Shards |> Array.Parallel.iter (fun x -> 
                                // This is to enable proper sorting
                                let topFieldCollector = TopFieldCollector.create (sort, count, true, true, true, true)
                                indexSearchers.[x.ShardNumber].search(query, topFieldCollector)
                                topDocsCollection.[x.ShardNumber] <- topFieldCollector.topDocs())
        let totalDocs = TopDocs.merge (sort, count, topDocsCollection)
        let hits = totalDocs.scoreDocs
        let searchResults = new SearchResults()
        searchResults.RecordsReturned <- totalDocs.scoreDocs.Count() - search.Skip
        searchResults.TotalAvailable <- totalDocs.totalHits
        let highlighterOptions = 
            if search.Highlights <> Unchecked.defaultof<_> then 
                match search.Highlights.HighlightedFields with
                | x when x.Count = 1 -> 
                    match flexIndex.IndexSetting.FieldsLookup.TryGetValue(x.First()) with
                    | (true, field) -> 
                        let htmlFormatter = new SimpleHTMLFormatter(search.Highlights.PreTag, search.Highlights.PostTag)
                        Some(field, new Highlighter(htmlFormatter, new QueryScorer(query)))
                    | _ -> None
                | _ -> None
            else None
        
        let mutable skipped : int = 0
        for hit in hits do
            if search.Skip > 0 && skipped < search.Skip then skipped <- skipped + 1
            else 
                let document = indexSearchers.[hit.shardIndex].doc(hit.doc)
                let flexDocument = new Document()
                if search.ReturnFlatResult <> true then 
                    flexDocument.Id <- document.get (Constants.IdField)
                    flexDocument.Index <- document.get (Constants.TypeField)
                    flexDocument.LastModified <- int64 (document.get (Constants.LastModifiedField))
                    if search.ReturnScore then flexDocument.Score <- float (hit.score)
                else 
                    flexDocument.Fields.Add(Constants.IdField, document.get (Constants.IdField))
                    flexDocument.Fields.Add(Constants.TypeField, document.get (Constants.TypeField))
                    flexDocument.Fields.Add(Constants.LastModifiedField, document.get (Constants.LastModifiedField))
                    if search.ReturnScore then flexDocument.Fields.Add("_score", hit.score.ToString())
                match search.Columns with
                // Return no other columns when nothing is passed
                | x when search.Columns.Count = 0 -> ()
                // Return all columns when *
                | x when search.Columns.First() = "*" -> 
                    for field in flexIndex.IndexSetting.Fields do
                        if field.FieldName = Constants.IdField || field.FieldName = Constants.TypeField 
                           || field.FieldName = Constants.LastModifiedField then ()
                        else 
                            let value = document.get (field.FieldName)
                            if value <> null then flexDocument.Fields.Add(field.FieldName, value)
                // Return only the requested columns
                | _ -> 
                    for fieldName in search.Columns do
                        let value = document.get (fieldName)
                        if value <> null then flexDocument.Fields.Add(fieldName, value)
                if highlighterOptions.IsSome then 
                    let (field, highlighter) = highlighterOptions.Value
                    let text = document.get (field.FieldName)
                    if text <> null then 
                        let tokenStream = 
                            TokenSources.getAnyTokenStream 
                                (indexSearchers.[hit.shardIndex].getIndexReader(), hit.doc, field.FieldName, 
                                 flexIndex.IndexSetting.SearchAnalyzer)
                        let frags = 
                            highlighter.getBestTextFragments 
                                (tokenStream, text, false, search.Highlights.FragmentsToReturn)
                        for frag in frags do
                            if frag <> null && frag.getScore() > float32 (0) then 
                                flexDocument.Highlights.Add(frag.ToString())
                searchResults.Documents.Add(flexDocument)
        for i in 0..indexSearchers.Count - 1 do
            (flexIndex.Shards.[i].NRTManager :> ReferenceManager).release(indexSearchers.[i])
        Choice1Of2 searchResults
    
    // ----------------------------------------------------------------------------
    // Search service class which will be dynamically injected using IOC. This will
    // provide the interface for all kind of search functionality in flex.
    // ----------------------------------------------------------------------------    
    type SearchService(queryTypes : Dictionary<string, IFlexQuery>, queryParsersPool : ObjectPool<FlexParser>) = 
        interface ISearchService with
            member x.Search(flexIndex : FlexIndex, search : SearchQuery) = 
                maybe { 
                    use parser = queryParsersPool.Acquire()
                    let! predicate = parser.Parse(search.QueryString)
                    parser.Release()
                    let! query = GenerateQuery flexIndex predicate search None queryTypes
                    return! SearchQuery(flexIndex, query, search)
                }
    
    // ----------------------------------------------------------------------------
    // Method responsible for generating top level boolean query for the search query
    // ----------------------------------------------------------------------------   
    //        let rec GenerateQueryFromFilter(flexIndex: FlexIndex, filter: SearchFilter, isTopLevelQuery, isProfileBased: KeyValuePairs option) =
    //            let query = new BooleanQuery()
    //            let occur = 
    //                if (filter.FilterType = FlexSearch.Api.FilterType.And) then
    //                    BooleanClause.Occur.MUST
    //                else
    //                    BooleanClause.Occur.SHOULD
    //            
    //            for condition in filter.Conditions do
    //                let mutable ignoreCondition : bool = false
    //
    //                match queryTypes.TryGetValue(condition.Operator) with
    //                | (true, queryType) ->
    //                    match flexIndex.IndexSetting.FieldsLookup.TryGetValue(condition.FieldName) with
    //                    | (true, field) -> 
    //                        match isProfileBased with
    //                        | Some(a) -> 
    //                            match a.TryGetValue(condition.FieldName) with
    //                            | (true, b) -> condition.Values.[0] = b |> ignore
    //                            | _ -> 
    //                                match condition.MissingValueOption with
    //                                | MissingValueOption.Ignore -> ignoreCondition <- true
    //                                | MissingValueOption.ThrowError -> failwithf "The specified condition: %s for the field %s does not have any values specified."  condition.Operator condition.FieldName
    //                                | MissingValueOption.Default -> () // We already have the default value in our condition  
    //                                | _ -> failwithf "The specified condition: %s for the field %s does not have any valid missing value option specified."  condition.Operator condition.FieldName
    //                        | None -> ()
    //
    //                        if ignoreCondition = false then
    //                            // Perform all check if the specified value is correct or not
    //                            if (condition.Values.Count = 0 || System.String.IsNullOrWhiteSpace(condition.Values.[0])) then 
    //                                failwithf "The specified condition: %s for the field %s does not have any values specified." condition.Operator condition.FieldName
    //                            
    //                            // Pre query generation validation
    //                            if field.StoreInformation.IsStoredOnly then failwithf "Store only fields cannot be searched: %s." field.FieldName
    //
    //                            match queryType.GetQuery(field, condition) with
    //                            | Some(a) -> 
    //
    //                                // Post query generation parameter setup
    //                                // Set the boost for the query
    //                                if condition.Boost > 1 then a.setBoost(float32(condition.Boost))
    //
    //                                // Add query to the top level boolean query
    //                                query.add(new BooleanClause(a, occur))
    //                            | None -> failwithf "Unable to generate query for the condition: %s and field %s" condition.Operator condition.FieldName
    //                    
    //                    | _ -> failwithf "The requested field does not exist in the index: %s." condition.FieldName
    //                | _ -> failwithf "The requested query does not exist: %s." condition.Operator
    //            
    //            if filter.SubFilters <> null then
    //                for subFilter in filter.SubFilters do
    //                    let subQuery = GenerateQueryFromFilter(flexIndex, subFilter, false, isProfileBased)
    //                    query.add(new BooleanClause(subQuery, occur))
    //            
    //            if filter.ConstantScore > 1 && isTopLevelQuery = false then
    //                let constantScoreQuery = new ConstantScoreQuery(query)
    //                constantScoreQuery.setBoost(float32(filter.ConstantScore))
    //                constantScoreQuery :> Query 
    //            else                    
    //                query :> Query
    //
    //
    //        let GetSearchProfileName(flexIndex: FlexIndex, searchProfile: SearchProfileQuery) =
    //            match flexIndex.IndexSetting.ScriptsManager.ProfileSelectorScripts.TryGetValue(searchProfile.SearchProfileName) with
    //            | (true, foo) -> 
    //                foo searchProfile.Fields
    //            | _ -> failwithf "The requested search profile selector does not exist."
    //
    //                        
    //        let GenerateQueryFromSearchProfile(flexIndex: FlexIndex, searchProfileQuery: SearchProfileQuery) =
    //            let searchProfileName =
    //                if String.IsNullOrWhiteSpace(searchProfileQuery.SearchProfileSelector) = false then
    //                    GetSearchProfileName(flexIndex, searchProfileQuery)
    //                elif String.IsNullOrEmpty(searchProfileQuery.SearchProfileName) = false then
    //                    searchProfileQuery.SearchProfileName
    //                else
    //                    failwithf "'SearchProfileName' cannot be empty."
    //            
    //            let searchProfile = 
    //                match flexIndex.IndexSetting.SearchProfiles.TryGetValue(searchProfileName) with
    //                | (true, b) -> b
    //                | _ -> failwithf  "The requested search profile selector does not exist."
    //            
    //            let query = GenerateQueryFromFilter(flexIndex, searchProfile.Query, true, Some(searchProfileQuery.Fields))
    //            (query, searchProfile)
    //           
    //
    //        interface ISearchService with
    //            member x.Search(flexIndex: FlexIndex, search: SearchQuery) =
    //                let query = GenerateQueryFromFilter(flexIndex, search.Query, true, None)
    //                SearchQuery(flexIndex, query, search)
    //            
    //            member this.SearchProfile(flexIndex: FlexIndex, searchProfile: SearchProfileQuery) =
    //                let (query, searchProfileQuery) = GenerateQueryFromSearchProfile(flexIndex, searchProfile)
    //                SearchQuery(flexIndex, query, searchProfileQuery)
    // Check if the passed field is numeic field
    let inline IsNumericField(flexField : FlexField) = 
        match flexField.FieldType with
        | FlexDate | FlexDateTime | FlexInt | FlexDouble -> true
        | _ -> false
    
    // Get a search query parser associated with the field 
    let inline GetSearchAnalyzer(flexField : FlexField) = 
        match flexField.FieldType with
        | FlexCustom(a, b) -> Some(a.SearchAnalyzer)
        | FlexHighlight(a) -> Some(a.SearchAnalyzer)
        | FlexText(a) -> Some(a.SearchAnalyzer)
        | FlexExactText(a) -> Some(a)
        | FlexBool(a) -> Some(a)
        | FlexDate | FlexDateTime | FlexInt | FlexDouble | FlexStored -> None
    
    // Find terms associated with the search string
    let inline GetTerms(flexField : FlexField, value) = 
        match GetSearchAnalyzer(flexField) with
        | Some(a) -> ParseTextUsingAnalyzer(a, flexField.FieldName, value)
        | None -> new List<string>([ value ])
    
    let getKeyValue (value : string) = 
        if (value.Contains(":")) then Some(value.Substring(0, value.IndexOf(":")), value.Substring(value.IndexOf(":") + 1))
        else None
    
    let inline getParametersAsDict (arr : string array) (skip : int) = 
        let parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        arr 
        |> Array.iteri 
               (fun i x -> 
               if i > skip && x.Contains(":") then 
                   parameters.Add(x.Substring(0, x.IndexOf(":")), x.Substring(x.IndexOf(":"))))
        parameters
    
    let NumericTermQuery(flexIndexField, value) = 
        match flexIndexField.FieldType with
        | FlexDate | FlexDateTime -> 
            match Int64.TryParse(value) with
            | (true, val1) -> 
                Some
                    (NumericRangeQuery.newLongRange 
                         (flexIndexField.FieldName, GetJavaLong(val1), GetJavaLong(val1), true, true) :> Query)
            | _ -> failwithf "Passed data is not in correct format."
        | FlexInt -> 
            match Int32.TryParse(value) with
            | (true, val1) -> 
                Some
                    (NumericRangeQuery.newIntRange 
                         (flexIndexField.FieldName, GetJavaInt(val1), GetJavaInt(val1), true, true) :> Query)
            | _ -> failwithf "Passed data is not in correct format."
        | FlexDouble -> 
            match Double.TryParse(value) with
            | (true, val1) -> 
                Some
                    (NumericRangeQuery.newDoubleRange 
                         (flexIndexField.FieldName, GetJavaDouble(val1), GetJavaDouble(val1), true, true) :> Query)
            | _ -> failwithf "Passed data is not in correct format."
        | _ -> failwith "Numeric range query is not supported on the passed data type."
    
    // ----------------------------------------------------------------------------
    // Term Query
    // ---------------------------------------------------------------------------- 
    [<Export(typeof<IFlexQuery>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "term_match")>]
    type FlexTermQuery() = 
        interface IFlexQuery with
            member this.QueryName() = [| "eq" |]
            member this.GetQuery(flexIndexField, values) = 
                match IsNumericField(flexIndexField) with
                | true -> Choice1Of2(NumericTermQuery(flexIndexField, values.[0]).Value)
                | false -> 
                    let terms = GetTerms(flexIndexField, values.[0])
                    // If there are multiple terms returned by the parser then we will create a boolean query
                    // with all the terms as sub clauses with And operator
                    // This behaviour will result in matching of both the terms in the results which may not be
                    // adjacent to each other. The adjaceny case should be handled through phrase query
                    match terms.Count with
                    | 0 -> Choice1Of2(new MatchAllDocsQuery() :> Query)
                    | 1 -> Choice1Of2(new TermQuery(new Term(flexIndexField.FieldName, terms.[0])) :> Query)
                    | _ -> 
                        // Generate boolean query
                        let boolClause = 
                            if values.Length > 1 then 
                                match getKeyValue (values.[1]) with
                                | Some(a, b) -> 
                                    match a with
                                    | InvariantEqual "clausetype" -> 
                                        match b with
                                        | InvariantEqual "or" -> BooleanClause.Occur.SHOULD
                                        | _ -> BooleanClause.Occur.MUST
                                    | _ -> BooleanClause.Occur.MUST
                                | _ -> BooleanClause.Occur.MUST
                            else BooleanClause.Occur.MUST
                        
                        let boolQuery = new BooleanQuery()
                        for term in terms do
                            boolQuery.add 
                                (new BooleanClause(new TermQuery(new Term(flexIndexField.FieldName, term)), boolClause))
                        Choice1Of2(boolQuery :> Query)
    
    // ----------------------------------------------------------------------------
    // Fuzzy Query
    // ---------------------------------------------------------------------------- 
    [<Export(typeof<IFlexQuery>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "fuzzy_match")>]
    type FlexFuzzyQuery() = 
        interface IFlexQuery with
            member this.QueryName() = [| "fuzzy" |]
            member this.GetQuery(flexIndexField, values) = 
                let terms = GetTerms(flexIndexField, values.[0])
                let parameters = getParametersAsDict values 1
                
                let slop = 
                    match parameters.TryGetValue("slop") with
                    | (true, value) -> 
                        match System.Int32.TryParse(value) with
                        | (true, result) -> result
                        | _ -> 1
                    | _ -> 1
                
                let prefixLength = 
                    match parameters.TryGetValue("prefixlength") with
                    | (true, value) -> 
                        match System.Int32.TryParse(value) with
                        | (true, result) -> result
                        | _ -> 0
                    | _ -> 0
                
                match terms.Count with
                | 0 -> Choice1Of2(new MatchAllDocsQuery() :> Query)
                | 1 -> 
                    Choice1Of2
                        (new FuzzyQuery(new Term(flexIndexField.FieldName, terms.[0]), slop, prefixLength) :> Query)
                | _ -> 
                    // Generate boolean query
                    let boolQuery = new BooleanQuery()
                    for term in terms do
                        boolQuery.add 
                            (new BooleanClause(new FuzzyQuery(new Term(flexIndexField.FieldName, term), slop, 
                                                              prefixLength), BooleanClause.Occur.MUST))
                    Choice1Of2(boolQuery :> Query)
    
    // ----------------------------------------------------------------------------
    // Phrase Query
    // ---------------------------------------------------------------------------- 
    [<Export(typeof<IFlexQuery>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "phrase_match")>]
    type FlexPhraseQuery() = 
        interface IFlexQuery with
            member this.QueryName() = [| "match" |]
            member this.GetQuery(flexIndexField, values) = 
                let terms = GetTerms(flexIndexField, values.[0])
                let query = new PhraseQuery()
                for term in terms do
                    query.add (new Term(flexIndexField.FieldName, term))
                if values.Length > 1 then 
                    let slop = 
                        match getKeyValue (values.[1]) with
                        | Some(a, b) -> 
                            match a with
                            | InvariantEqual "slop" -> 
                                match System.Int32.TryParse(b) with
                                | (true, result) -> result
                                | _ -> 0
                            | _ -> 0
                        | _ -> 0
                    query.setSlop (slop)
                Choice1Of2(query :> Query)
//
//
//    // ----------------------------------------------------------------------------
//    // Wildcard Query
//    // ---------------------------------------------------------------------------- 
//    [<Export(typeof<IFlexQuery>)>]
//    [<PartCreationPolicy(CreationPolicy.NonShared)>]
//    [<ExportMetadata("Name", "like")>]
//    type FlexWildcardQuery() =
//        interface IFlexQuery with
//            //member this.QueryName() = [|"like"|]
//            member this.GetQuery(flexIndexField, condition) =
//                let terms = GetTerms(flexIndexField, condition.Values.[0])
//
//                match terms.Count with
//                | 0 -> None
//                | 1 -> Some(new WildcardQuery(new Term(flexIndexField.FieldName, terms.[0]))  :> Query)
//                | _ ->
//                    
//                    // Generate boolean query
//                    let boolQuery = new BooleanQuery()
//                    for term in terms do
//                        boolQuery.add(new WildcardQuery(new Term(flexIndexField.FieldName, term)), BooleanClause.Occur.MUST)
//                    Some(boolQuery :> Query)
//                
//
//    // ----------------------------------------------------------------------------
//    // Term Range Query
//    // ---------------------------------------------------------------------------- 
//    [<Export(typeof<IFlexQuery>)>]
//    [<PartCreationPolicy(CreationPolicy.NonShared)>]
//    [<ExportMetadata("Name", "string_range")>]
//    type FlexStringRangeQuery() =
//        interface IFlexQuery with
//            //member this.QueryName() = [|"string_range"|]
//            member this.GetQuery(flexIndexField, condition) =
//                if condition.Values.[0] = condition.Values.[1] then failwithf "Upper and lower limit of range query cannot be equal."
//
//                let term1 = GetTerms(flexIndexField, condition.Values.[0])
//                let term2 = GetTerms(flexIndexField, condition.Values.[1])
//
//                let includeLower =
//                    match condition.Parameters.TryGetValue("includelower")  with
//                    | (true, value) -> 
//                        match System.Boolean.TryParse(value) with
//                        | (true, result) -> result
//                        | _ -> false
//                    | _ -> false      
//
//                let includeUpper =
//                    match condition.Parameters.TryGetValue("includeupper")  with
//                    | (true, value) -> 
//                        match System.Boolean.TryParse(value) with
//                        | (true, result) -> result
//                        | _ -> false
//                    | _ -> false    
//
//                Some(TermRangeQuery.newStringRange(flexIndexField.FieldName, term1.[0], term2.[1], includeLower, includeUpper) :> Query)
//
//
//    // ----------------------------------------------------------------------------
//    // Term Range Query
//    // ---------------------------------------------------------------------------- 
//    [<Export(typeof<IFlexQuery>)>]
//    [<PartCreationPolicy(CreationPolicy.NonShared)>]
//    [<ExportMetadata("Name", "numeric_range")>]
//    type FlexNumericRangeQuery() =
//        interface IFlexQuery with
//            //member this.QueryName() = [|"numeric_range"|]
//            member this.GetQuery(flexIndexField, condition) =
//                if condition.Values.[0] = condition.Values.[1] then failwithf "Upper and lower limit of range query cannot be equal."
//
//                let term1 = condition.Values.[0]
//                let term2 = condition.Values.[1]
//
//                let includeLower =
//                    match condition.Parameters.TryGetValue("includelower")  with
//                    | (true, value) -> 
//                        match System.Boolean.TryParse(value) with
//                        | (true, result) -> result
//                        | _ -> false
//                    | _ -> false      
//
//                let includeUpper =
//                    match condition.Parameters.TryGetValue("includeupper")  with
//                    | (true, value) -> 
//                        match System.Boolean.TryParse(value) with
//                        | (true, result) -> result
//                        | _ -> false
//                    | _ -> false    
//                
//                match flexIndexField.FieldType with
//                | FlexDate
//                | FlexDateTime ->
//                    match (Int64.TryParse(condition.Values.[0]), Int64.TryParse(condition.Values.[1])) with
//                    | ((true, val1), (true, val2)) -> Some(NumericRangeQuery.newLongRange(flexIndexField.FieldName, GetJavaLong(val1), GetJavaLong(val2), includeLower, includeUpper) :> Query)
//                    | _ -> failwithf "Passed data is not in correct format."
//                | FlexInt -> 
//                    match (Int32.TryParse(condition.Values.[0]), Int32.TryParse(condition.Values.[1])) with
//                    | ((true, val1), (true, val2)) -> Some(NumericRangeQuery.newIntRange(flexIndexField.FieldName, GetJavaInt(val1), GetJavaInt(val2), includeLower, includeUpper) :> Query)
//                    | _ -> failwithf "Passed data is not in correct format."
//                | FlexDouble -> 
//                    match (Double.TryParse(condition.Values.[0]), Double.TryParse(condition.Values.[1])) with
//                    | ((true, val1), (true, val2)) -> Some(NumericRangeQuery.newDoubleRange(flexIndexField.FieldName, GetJavaDouble(val1), GetJavaDouble(val2), includeLower, includeUpper) :> Query)
//                    | _ -> failwithf "Passed data is not in correct format."
//                | _ -> failwith "Numeric range query is not supported on the passed data type."
