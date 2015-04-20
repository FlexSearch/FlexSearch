// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexLucene.Document
open FlexLucene.Index
open FlexLucene.Search
open FlexLucene.Search.Highlight
open FlexSearch.Core
open System
open System.Collections.Generic
open System.Linq

/// FlexQuery interface     
type IFlexQuery = 
    abstract QueryName : unit -> string []
    abstract GetQuery : Field.T * string [] * Dictionary<string, string> option -> Choice<Query, Error>

// ----------------------------------------------------------------------------
// Contains all predefined flex queries. Also contains the search factory service.
// The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
[<AutoOpen>]
module SearchDsl = 
    let inline queryNotFound queryName = QueryNotFound <| queryName
    let inline fieldNotFound fieldName = InvalidFieldName <| fieldName
    
    let generateQuery (fields : IReadOnlyDictionary<string, Field.T>, predicate : Predicate, searchQuery : SearchQuery.Dto, 
                       isProfileBased : Dictionary<string, string> option, queryTypes : Dictionary<string, IFlexQuery>) = 
        assert (queryTypes.Count > 0)
        let generateMatchAllQuery = ref false
        
        let getValue (fieldName, operator, v : Value, p) = 
            maybe { 
                match isProfileBased with
                | Some(source) -> 
                    match source.TryGetValue(fieldName) with
                    | true, v' -> return [| v' |]
                    | _ -> 
                        match searchQuery.MissingValueConfiguration.TryGetValue(fieldName) with
                        | true, configuration -> 
                            match configuration with
                            | MissingValueOption.Default -> return! v.GetValueAsArray()
                            | MissingValueOption.ThrowError -> return! fail (MissingFieldValue(fieldName))
                            | MissingValueOption.Ignore -> 
                                generateMatchAllQuery := true
                                return [| "" |]
                            | _ -> return! fail (UnknownMissingVauleOption(fieldName))
                        | _ -> 
                            // Check if a non blank value is provided as a part of the query
                            return! v.GetValueAsArray()
                | None -> return! v.GetValueAsArray()
            }
        
        /// Generate the query from the condition
        let getCondition (fieldName, operator, v : Value, p) = 
            maybe { 
                let! field = fields |> keyExists2 (fieldName, fieldNotFound)
                do! FieldType.searchable field.FieldType |> boolToResult (StoredFieldCannotBeSearched(field.FieldName))
                let! query = queryTypes |> keyExists (operator, queryNotFound)
                let! value = getValue (fieldName, operator, v, p)
                if generateMatchAllQuery.Value = true then return! ok <| getMatchAllDocsQuery()
                else 
                    let! q = query.GetQuery(field, value.ToArray(), p)
                    q.SetBoost(float32 <| doubleFromOptDict "boost" 1.0 p)
                    return q
            }
        
        /// Main rec function responsible for generating predicate
        let rec generateQuery (pred : Predicate) = 
            maybe { 
                match pred with
                | NotPredicate(pr) -> let! notQuery = generateQuery (pr)
                                      return getBooleanQuery() |> addMustNotClause notQuery :> Query
                | Condition(f, o, v, p) -> return! getCondition (f, o, v, p)
                | OrPredidate(lhs, rhs) -> 
                    let! lhsQuery = generateQuery (lhs)
                    let! rhsQuery = generateQuery (rhs)
                    return getBooleanQuery()
                           |> addShouldClause lhsQuery
                           |> addShouldClause rhsQuery :> Query
                | AndPredidate(lhs, rhs) -> 
                    let! lhsQuery = generateQuery (lhs)
                    let! rhsQuery = generateQuery (rhs)
                    return getBooleanQuery()
                           |> addMustClause lhsQuery
                           |> addMustClause rhsQuery :> Query
            }
        
        generateQuery predicate
    
    let inline search (indexWriter : IndexWriter.T, query : Query, search : SearchQuery.Dto) = 
        let indexSearchers = indexWriter |> IndexWriter.getRealTimeSearchers
        // Each thread only works on a separate part of the array and as no parts are shared across
        // multiple threads the below variables are thread safe. The cost of using blocking collection vs. 
        // array per search is high
        let topDocsCollection : TopFieldDocs array = Array.zeroCreate indexSearchers.Length
        
        let sort = 
            match search.OrderBy with
            | null -> Sort.RELEVANCE
            | _ -> 
                match indexWriter.Settings.FieldsLookup.TryGetValue(search.OrderBy) with
                | (true, field) -> new Sort(new SortField(field.SchemaName, FieldType.sortField field.FieldType))
                | _ -> Sort.RELEVANCE
        
        let count = 
            match search.Count with
            | 0 -> 10 + search.Skip
            | _ -> search.Count + search.Skip
        
        indexWriter.ShardWriters |> Array.Parallel.iter (fun x -> 
                                        // This is to enable proper sorting
                                        let topFieldCollector = 
                                            TopFieldCollector.Create(sort, count, null, true, true, true)
                                        indexSearchers.[x.ShardNo].IndexSearcher.Search(query, topFieldCollector)
                                        topDocsCollection.[x.ShardNo] <- topFieldCollector.TopDocs())
        let totalDocs = TopDocs.Merge(sort, count, topDocsCollection)
        let hits = totalDocs.ScoreDocs
        let recordsReturned = totalDocs.ScoreDocs.Count() - search.Skip
        let totalAvailable = totalDocs.TotalHits
        
        let highlighterOptions = 
            if search.Highlights <> Unchecked.defaultof<_> then 
                match search.Highlights.HighlightedFields with
                | x when x.Length = 1 -> 
                    match indexWriter.Settings.FieldsLookup.TryGetValue(x.First()) with
                    | (true, field) -> 
                        let htmlFormatter = new SimpleHTMLFormatter(search.Highlights.PreTag, search.Highlights.PostTag)
                        Some(field, new Highlighter(htmlFormatter, new QueryScorer(query)))
                    | _ -> None
                | _ -> None
            else None
        (hits, highlighterOptions, recordsReturned, totalAvailable, indexSearchers)
    
    /// Returns a document from the index
    let getDocument (indexWriter : IndexWriter.T, search : SearchQuery.Dto, document : Document) = 
        let fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        match search.Columns with
        // Return no other columns when nothing is passed
        | _ when search.Columns.Length = 0 -> ()
        // Return all columns when *
        | _ when search.Columns.First() = "*" -> 
            for field in indexWriter.Settings.Fields do
                if field.FieldName = Constants.IdField || field.FieldName = Constants.LastModifiedField then ()
                else 
                    let value = document.Get(field.SchemaName)
                    if value <> null then fields.Add(field.FieldName, value)
        // Return only the requested columns
        | _ -> 
            for fieldName in search.Columns do
                match indexWriter.Settings.FieldsLookup.TryGetValue(fieldName) with
                | (true, field) -> 
                    let value = document.Get(field.SchemaName)
                    if value <> null then fields.Add(field.FieldName, value)
                | _ -> ()
        fields
    
    /// Represents an empty list
    let emptyList = Enumerable.Empty<string>().ToList()
    
    /// Searches for documents over the index using the query and returns the documents as a sequence
    let searchDocumentSeq (indexWriter : IndexWriter.T, query : Query, searchQuery : SearchQuery.Dto) = 
        let (hits, highlighterOptions, recordsReturned, totalAvailable, indexSearchers) = 
            search (indexWriter, query, searchQuery)
        
        let inline getHighlighter (document : Document, shardIndex, doc) = 
            if highlighterOptions.IsSome then 
                let highlights = new List<string>()
                let (field, highlighter) = highlighterOptions.Value
                let text = document.Get(field.SchemaName)
                if text <> null then 
                    let tokenStream = 
                        TokenSources.GetAnyTokenStream
                            (indexSearchers.[shardIndex].IndexReader, doc, field.SchemaName, 
                             indexWriter.Settings.SearchAnalyzer)
                    let frags = 
                        highlighter.GetBestTextFragments
                            (tokenStream, text, false, searchQuery.Highlights.FragmentsToReturn)
                    for frag in frags do
                        if frag <> null && frag.GetScore() > float32 (0) then highlights.Add(frag.ToString())
                highlights
            else emptyList
        
        let results = 
            seq { 
                for i = searchQuery.Skip to hits.Length - 1 do
                    let hit = hits.[i]
                    let document = indexSearchers.[hit.ShardIndex].IndexSearcher.Doc(hit.Doc)
                    let fields = getDocument (indexWriter, searchQuery, document)
                    let resultDoc = new Document.Dto()
                    resultDoc.Id <- document.Get(indexWriter.GetSchemaName(Constants.IdField))
                    resultDoc.IndexName <- indexWriter.Settings.IndexName
                    resultDoc.TimeStamp <- int64 (document.Get(indexWriter.GetSchemaName(Constants.LastModifiedField)))
                    resultDoc.Fields <- fields
                    resultDoc.Score <- if searchQuery.ReturnScore then float (hit.Score) else 0.0
                    resultDoc.Highlights <- getHighlighter (document, hit.ShardIndex, hit.Doc)
                    yield resultDoc
            }
        
        for i in 0..indexSearchers.Length - 1 do
            (indexSearchers.[i] :> IDisposable).Dispose()
        ok (results, recordsReturned, totalAvailable)
    
    /// Searches for documents over the index using the query and returns the documents as a sequence of 
    /// dictionary    
    let searchDictionarySeq (indexWriter : IndexWriter.T, query : Query, searchQuery : SearchQuery.Dto) = 
        let (hits, _, recordsReturned, totalAvailable, indexSearchers) = search (indexWriter, query, searchQuery)
        
        let results = 
            seq { 
                for i = searchQuery.Skip to hits.Length - 1 do
                    let hit = hits.[i]
                    let document = indexSearchers.[hit.ShardIndex].IndexSearcher.Doc(hit.Doc)
                    let fields = getDocument (indexWriter, searchQuery, document)
                    fields.Add(Constants.IdField, document.Get(indexWriter.GetSchemaName(Constants.IdField)))
                    fields.Add
                        (Constants.LastModifiedField, 
                         document.Get(indexWriter.GetSchemaName(Constants.LastModifiedField)))
                    if searchQuery.ReturnScore then fields.Add("_score", hit.Score.ToString())
                    yield fields
            }
        for i in 0..indexSearchers.Length - 1 do
            (indexSearchers.[i] :> IDisposable).Dispose()
        ok (results, recordsReturned, totalAvailable)

/// Term Query
[<Name("term_match"); Sealed>]
type FlexTermQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| "eq"; "=" |]
        member __.GetQuery(flexIndexField, values, parameters) = 
            match FieldType.isNumericField (flexIndexField.FieldType) with
            | true -> getRangeQuery values.[0] (true, true) (NoInfinite, NoInfinite) flexIndexField
            | false -> 
                // If there are multiple terms returned by the parser then we will create a boolean query
                // with all the terms as sub clauses with And operator
                // This behaviour will result in matching of both the terms in the results which may not be
                // adjacent to each other. The adjacency case should be handled through phrase query
                zeroOneOrManyQuery <| getTerms (flexIndexField, values.[0]) <| getTermQuery flexIndexField.SchemaName 
                <| getBooleanClause parameters

/// Fuzzy Query
[<Name("fuzzy_match"); Sealed>]
type FlexFuzzyQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| "fuzzy"; "~=" |]
        member __.GetQuery(flexIndexField, values, parameters) = 
            let slop = parameters |> intFromOptDict "slop" 1
            let prefixLength = parameters |> intFromOptDict "prefixlength" 0
            zeroOneOrManyQuery <| getTerms (flexIndexField, values.[0]) 
            <| getFuzzyQuery flexIndexField.SchemaName slop prefixLength <| BooleanClause.Occur.MUST

/// Match all Query
[<Name("match_all"); Sealed>]
type FlexMatchAllQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| "matchall" |]
        member __.GetQuery(_, _, _) = ok <| getMatchAllDocsQuery()

/// Phrase Query
[<Name("phrase_match"); Sealed>]
type FlexPhraseQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| "match" |]
        member __.GetQuery(flexIndexField, values, parameters) = 
            let terms = getTerms (flexIndexField, values.[0])
            let query = new PhraseQuery()
            for term in terms do
                query.Add(new Term(flexIndexField.SchemaName, term))
            let slop = parameters |> intFromOptDict "slop" 0
            query.SetSlop(slop)
            ok <| (query :> Query)

/// Wildcard Query
[<Name("like"); Sealed>]
type FlexWildcardQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| "like"; "%=" |]
        member __.GetQuery(flexIndexField, values, _) = 
            // Like query does not go through analysis phase as the analyzer would remove the
            // special character
            zeroOneOrManyQuery <| (values |> Seq.map (fun x -> x.ToLowerInvariant())) 
            <| getWildCardQuery flexIndexField.SchemaName <| BooleanClause.Occur.MUST

/// Regex Query
[<Name("regex"); Sealed>]
type RegexQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| "regex" |]
        member __.GetQuery(flexIndexField, values, _) = 
            // Regex query does not go through analysis phase as the analyzer would remove the
            // special character
            zeroOneOrManyQuery <| (values |> Seq.map (fun x -> x.ToLowerInvariant())) 
            <| getRegexpQuery flexIndexField.SchemaName <| BooleanClause.Occur.MUST

// ----------------------------------------------------------------------------
// Range Queries
// Note: These queries don't go through analysis phase as the analyzer would 
// remove the special character
// ---------------------------------------------------------------------------- 
[<Name("greater"); Sealed>]
type FlexGreaterQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| ">" |]
        member __.GetQuery(flexIndexField, values, _) = 
            getRangeQuery values.[0] (false, true) (NoInfinite, MaxInfinite) flexIndexField

[<Name("greater_than_equal"); Sealed>]
type FlexGreaterThanEqualQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| ">=" |]
        member __.GetQuery(flexIndexField, values, _) = 
            getRangeQuery values.[0] (true, true) (NoInfinite, MaxInfinite) flexIndexField

[<Name("less_than"); Sealed>]
type FlexLessThanQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| "<" |]
        member __.GetQuery(flexIndexField, values, _) = 
            getRangeQuery values.[0] (true, false) (MinInfinite, NoInfinite) flexIndexField

[<Name("less_than_equal"); Sealed>]
type FlexLessThanEqualQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| "<=" |]
        member __.GetQuery(flexIndexField, values, _) = 
            getRangeQuery values.[0] (true, true) (MinInfinite, NoInfinite) flexIndexField
