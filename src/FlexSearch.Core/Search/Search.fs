// ----------------------------------------------------------------------------
// FlexSearch predefined tokenizers (Tokenizers.fs)
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
open FlexSearch.Api.Validation
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
open FlexSearch.Common
// ----------------------------------------------------------------------------
// Contains all predefined flex queries. Also contains the search factory service.
// The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
[<AutoOpen>]
module SearchDsl = 
    let FlexCharTermAttribute = 
        lazy java.lang.Class.forName 
                 (typeof<org.apache.lucene.analysis.tokenattributes.CharTermAttribute>.AssemblyQualifiedName)
    
    /// Utility function to get tokens from the search string based upon the passed analyzer
    /// This will enable us to avoid using the Lucene query parser
    /// We cannot use simple white space based token generation as it really depends 
    /// upon the analyzer used
    let inline ParseTextUsingAnalyzer(analyzer : Analyzer, fieldName, queryText) = 
        let tokens = new List<string>()
        let source : TokenStream = analyzer.tokenStream (fieldName, new StringReader(queryText))
        // Get the CharTermAttribute from the TokenStream
        let termAtt = source.addAttribute (FlexCharTermAttribute.Value)
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
    
    let private IsStoredField(flexField : FlexField) = 
        match flexField.FieldType with
        | FlexFieldType.FlexStored -> 
            Choice2Of2(Errors.STORED_FIELDS_CANNOT_BE_SEARCHED
                       |> GenerateOperationMessage
                       |> Append("Field Name", flexField.FieldName))
        | _ -> Choice1Of2()
    
    let inline GetIntValueFromMap (parameters : Map<string, string> option) key defaultValue = 
        match parameters with
        | Some(p) -> 
            match p.TryFind(key) with
            | Some(value) -> 
                match System.Int32.TryParse(value) with
                | (true, result) -> result
                | _ -> defaultValue
            | _ -> defaultValue
        | _ -> defaultValue
    
    let inline GetStringValueFromMap (parameters : Map<string, string> option) key defaultValue = 
        match parameters with
        | Some(p) -> 
            match p.TryFind(key) with
            | Some(value) -> 
                if String.IsNullOrWhiteSpace(value) then value
                else defaultValue
            | _ -> defaultValue
        | _ -> defaultValue
    
    let inline GetBoolValueFromMap (parameters : Map<string, string> option) key defaultValue = 
        match parameters with
        | Some(p) -> 
            match p.TryFind(key) with
            | Some(value) -> 
                match System.Boolean.TryParse(value) with
                | (true, result) -> result
                | _ -> defaultValue
            | _ -> defaultValue
        | _ -> defaultValue
    
    let GenerateQuery(fields : Dictionary<string, FlexField>, predicate : Predicate, searchQuery : SearchQuery, 
                      isProfileBased : Map<string, string> option, queryTypes : Dictionary<string, IFlexQuery>) = 
        assert (queryTypes.Count > 0)
        let rec generateQuery (pred : Predicate) = 
            maybe { 
                let generateMatchAllQuery = ref false
                match pred with
                | NotPredicate(pr) -> 
                    let! notQuery = generateQuery (pr)
                    let query = new BooleanQuery()
                    query.add (new BooleanClause(notQuery, BooleanClause.Occur.MUST_NOT))
                    return (query :> Query)
                | Condition(f, o, v, p) -> 
                    let! field = KeyExists(f, fields, Errors.INVALID_FIELD_NAME |> GenerateOperationMessage)
                    do! IsStoredField field
                    let! query = KeyExists(o, queryTypes, Errors.INVALID_QUERY_TYPE |> GenerateOperationMessage)
                    let! value = maybe { 
                                     match isProfileBased with
                                     | Some(source) -> 
                                         match source.TryFind(f) with
                                         | Some(v') -> return [| v' |]
                                         | _ -> 
                                             match searchQuery.MissingValueConfiguration.TryGetValue(f) with
                                             | true, configuration -> 
                                                 match configuration with
                                                 | MissingValueOption.Default -> return! v.GetValueAsArray()
                                                 | MissingValueOption.ThrowError -> 
                                                     return! Choice2Of2(Errors.MISSING_FIELD_VALUE_1
                                                                        |> GenerateOperationMessage
                                                                        |> Append("Field Name", f))
                                                 | MissingValueOption.Ignore -> 
                                                     generateMatchAllQuery := true
                                                     return [| "" |]
                                                 | _ -> 
                                                     return! Choice2Of2(Errors.UNKNOWN_MISSING_VALUE_OPTION
                                                                        |> GenerateOperationMessage
                                                                        |> Append("Field Name", f))
                                             | _ -> 
                                                 // Check if a non blank value is provided as a part of the query
                                                 return! v.GetValueAsArray()
                                     | None -> return! v.GetValueAsArray()
                                 }
                    if generateMatchAllQuery.Value = true then return! Choice1Of2(new MatchAllDocsQuery() :> Query)
                    else 
                        let! q = query.GetQuery(field, value.ToArray(), p)
                        match p with
                        | Some(p') -> 
                            match p'.TryFind("boost") with
                            | Some(b) -> 
                                match Int32.TryParse(b) with
                                | true, b' -> q.setBoost (float32 (b'))
                                | _ -> ()
                            | _ -> ()
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
    
    let SearchQuery(flexIndex : FlexIndex, query : Query, search : SearchQuery) = 
        let indexSearchers = new List<IndexSearcher>()
        for i in 0..flexIndex.Shards.Length - 1 do
            let searcher = (flexIndex.Shards.[i].NRTManager :> ReferenceManager).acquire() :?> IndexSearcher
            indexSearchers.Add(searcher)
        // Each thread only works on a separate part of the array and as no parts are shared across
        // multiple threads the below variables are thread safe. The cost of using blocking collection vs. 
        // array per search is high
        let topDocsCollection : TopDocs array = Array.zeroCreate indexSearchers.Count
        
        let sort = 
            match search.OrderBy with
            | null -> Sort.RELEVANCE
            | _ -> 
                match flexIndex.IndexSetting.FieldsLookup.TryGetValue(search.OrderBy) with
                | (true, field) -> new Sort(new SortField(field.SchemaName, FlexField.SortField(field)))
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
                    flexDocument.Id <- document.get (flexIndex.IndexSetting.FieldsLookup.[Constants.IdField].SchemaName)
                    flexDocument.Index <- flexIndex.IndexSetting.IndexName
                    flexDocument.LastModified <- int64 
                                                     (document.get 
                                                          (flexIndex.IndexSetting.FieldsLookup.[Constants.LastModifiedField].SchemaName))
                    if search.ReturnScore then flexDocument.Score <- float (hit.score)
                else 
                    flexDocument.Fields.Add
                        (Constants.IdField, 
                         document.get (flexIndex.IndexSetting.FieldsLookup.[Constants.IdField].SchemaName))
                    flexDocument.Fields.Add(Constants.TypeField, flexIndex.IndexSetting.IndexName)
                    flexDocument.Fields.Add
                        (Constants.LastModifiedField, 
                         document.get (flexIndex.IndexSetting.FieldsLookup.[Constants.LastModifiedField].SchemaName))
                    if search.ReturnScore then flexDocument.Fields.Add("_score", hit.score.ToString())
                match search.Columns with
                // Return no other columns when nothing is passed
                | x when search.Columns.Count = 0 -> ()
                // Return all columns when *
                | x when search.Columns.First() = "*" -> 
                    for field in flexIndex.IndexSetting.Fields do
                        if field.FieldName = Constants.IdField || field.FieldName = Constants.LastModifiedField then ()
                        else 
                            let value = document.get (field.SchemaName)
                            if value <> null then flexDocument.Fields.Add(field.FieldName, value)
                // Return only the requested columns
                | _ -> 
                    for fieldName in search.Columns do
                        match flexIndex.IndexSetting.FieldsLookup.TryGetValue(fieldName) with
                        | (true, field) -> 
                            let value = document.get (field.SchemaName)
                            if value <> null then flexDocument.Fields.Add(field.FieldName, value)
                        | _ -> ()
                if highlighterOptions.IsSome then 
                    let (field, highlighter) = highlighterOptions.Value
                    let text = document.get (field.SchemaName)
                    if text <> null then 
                        let tokenStream = 
                            TokenSources.getAnyTokenStream 
                                (indexSearchers.[hit.shardIndex].getIndexReader(), hit.doc, field.SchemaName, 
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
