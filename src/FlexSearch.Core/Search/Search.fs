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

open FlexSearch.Api
open FlexSearch.Api.Model
open FlexLucene.Document
open FlexLucene.Index
open FlexLucene.Search
open FlexLucene.Search.Highlight
open FlexSearch.Core
open System
open System.Collections.Generic
open System.Linq
open System.ComponentModel.Composition

type ComputedValues = string option []
type ComputedValue = string option
type Variables = Dictionary<string, string>

// The reason why we also have the function name as a parameter is that the function name from the 
// query string might be different than the name of the actual IFieldFunction instance.
// E.g. function name from query string: upTo2WordsApart
//      function name from IFieldFunction instance : upToWordsApart
// This enables us to take the number from the function name and use it as a parameter
type IFieldFunction = abstract GetQuery : FieldSchema * ComputedValues * FunctionName -> Result<Query>
type IComputedFunction = abstract GetQuery : ComputedValues -> Result<ComputedValue>
type IQueryFunction = abstract GetQuery : Query * ComputedValue -> Result<Query>

type SearchBaggage = 
  { Fields : IReadOnlyDictionary<string, FieldSchema>
    ComputedFunctions : Dictionary<string, IComputedFunction>
    FieldFunctions : Dictionary<string, IFieldFunction>
    QueryFunctions : Dictionary<string, IQueryFunction> }

// ----------------------------------------------------------------------------
// Contains all predefined flex queries. Also contains the search factory service.
// The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
[<AutoOpen>]
module SearchDsl = 
    let inline queryNotFound queryName = QueryNotFound <| queryName
    let inline fieldNotFound fieldName = InvalidFieldName <| fieldName
    let inline extractFunctionName (str : string) = 
        str |> Seq.where (fun c -> Char.IsLetter c || c = '_') 
            |> String.Concat
            |> fun s -> s.ToLower()

    let getFunction<'T> (name : string) (functions : Dictionary<string, 'T>) =
        match functions.TryGetValue <| extractFunctionName name with
        | true, func -> ok func
        | _ -> fail <| FunctionNotFound name

    let getFieldSchema fieldName (fields : IReadOnlyDictionary<string, FieldSchema>) =
        maybe {
            let! field = fields |> keyExists2 (fieldName, fieldNotFound)
            do! FieldSchema.isSearchable field |> boolToResult (StoredFieldCannotBeSearched(field.FieldName))
            return field }

    let getFieldDefaultValue fieldName (fields : IReadOnlyDictionary<string, FieldSchema>) =
        fields |> keyExists2 (fieldName, fieldNotFound)
        >>= fun field -> field.FieldType.DefaultStringValue |> ok

    // Gets a variable value from the search query.
    // Empty/blank values are considered an error
    let getVariable (name : VariableName) (variables : Variables) =
        match variables.TryGetValue <| name.ToLower() with
        | true, value -> 
            if isNotBlank value then ok value
            else fail <| MissingVariableValue name
        | _ -> fail <| MissingVariableValue name

    let rec getFieldNameFromFunction (func : Function) =
        match func with
        | FieldFunction(_,fieldName,_) -> fieldName
        | QueryFunction(_,func',_) -> getFieldNameFromFunction func'

    let rec computeValue baggage variables (fieldNameFromContext : FieldName) (computableValue : ComputableValue) = 
        match computableValue with
        | Constant(value) -> value |> Some |> ok
        | Variable(variableName) -> 
            match variableName.ToUpper() with
            | "IGNORE" -> ok None
            | "DEFAULT" -> baggage.Fields |> getFieldDefaultValue fieldNameFromContext
                           >>= (Some >> ok)
            | _ -> variables |> getVariable variableName
                   >>= (Some >> ok)
        | ComputableFunction(funcName, cvs) -> 
            cvs
            >>>= (computeValue baggage variables fieldNameFromContext)
            >>= (Seq.toArray >> ok)
            >>= fun computedValues -> 
                    baggage.ComputedFunctions |> getFunction funcName
                    >>= fun computedFunction -> computedFunction.GetQuery computedValues

    let computeFieldFunc baggage funcName fieldName cvs = 
        maybe {
            let! fieldSchema = baggage.Fields |> getFieldSchema fieldName
            let! searchFunction = baggage.FieldFunctions |> getFunction funcName
            return! searchFunction.GetQuery(fieldSchema, cvs, funcName)
        }

    let computeQueryFunction baggage funcName query computedValue = 
        baggage.QueryFunctions |> getFunction funcName
        >>= fun searchFunction -> searchFunction.GetQuery(query, computedValue)

    let rec compute (baggage : SearchBaggage) (variables : Variables) (func : Function) =
        match func with
        | FieldFunction(funcName, fieldName, cvs) ->
            cvs
            >>>= (computeValue baggage variables fieldName )
            >>= (Seq.toArray >> ok)
            >>= (computeFieldFunc baggage funcName fieldName)
        | QueryFunction(funcName, ``function``, value) ->
            ``function`` 
            |> compute baggage variables
            >>= fun query -> computeValue baggage variables (getFieldNameFromFunction ``function``) value
                             >>= fun computedValue -> (query, computedValue) |> ok
            >>= fun (q,cv) -> computeQueryFunction baggage funcName q cv

    let rec generateQuery (predicate : Predicate)
                          (searchQuery : SearchQuery)
                          (baggage : SearchBaggage) =
        assert (baggage.FieldFunctions.Count > 0)

        maybe {
            match predicate with
            | NotPredicate(pr) -> let! notQuery = generateQuery pr searchQuery baggage
                                  return getBooleanQuery() |> addMustNotClause notQuery
                                         :> Query
            | Clause(func) -> return! func |> compute baggage searchQuery.Variables
            | OrPredidate(lhs, rhs) -> 
                let! lhsQuery = generateQuery lhs searchQuery baggage
                let! rhsQuery = generateQuery rhs searchQuery baggage
                return getBooleanQuery()
                        |> addShouldClause lhsQuery
                        |> addShouldClause rhsQuery 
                        :> Query
            | AndPredidate(lhs, rhs) -> 
                let! lhsQuery = generateQuery lhs searchQuery baggage
                let! rhsQuery = generateQuery rhs searchQuery baggage
                return getBooleanQuery()
                        |> addMustClause lhsQuery
                        |> addMustClause rhsQuery 
                        :> Query }


    /// Returns a document from the index
    let getDocument (indexWriter : IndexWriter, search : SearchQuery, document : LuceneDocument) = 
        let fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        let getValue(field: FieldSchema) =
            let value = document.Get(field.SchemaName)
            if notNull value then 
                if value = Constants.StringDefaultValue && search.ReturnEmptyStringForNull then
                    fields.Add(field.FieldName, String.Empty)
                else
                    fields.Add(field.FieldName, value)

        match search.Columns with
        // Return no other columns when nothing is passed
        | _ when search.Columns.Length = 0 -> ()
        // Return all columns when *
        | _ when search.Columns.First() = "*" -> 
            for field in indexWriter.Settings.Fields do
                if [IdField.Name; TimeStampField.Name; ModifyIndexField.Name; StateField.Name]
                   |> Seq.contains field.FieldName
                then ()
                else getValue(field)
        // Return only the requested columns
        | _ -> 
            for fieldName in search.Columns do
                match indexWriter.Settings.Fields.TryGetValue(fieldName) with
                | (true, field) -> getValue(field)
                | _ -> ()
        fields
    
    let search (indexWriter : IndexWriter, query : Query, searchQuery : SearchQuery) = 
        (!>) "Input Query:%s \nGenerated Query : %s" (searchQuery.QueryString) (query.ToString())
        let indexSearchers = indexWriter |> IndexWriter.getRealTimeSearchers
        // Each thread only works on a separate part of the array and as no parts are shared across
        // multiple threads the below variables are thread safe. The cost of using blocking collection vs. 
        // array per search is high
        let topDocsCollection : TopFieldDocs array = Array.zeroCreate indexSearchers.Length
        
        let sortOrder =
            match searchQuery.OrderByDirection with
            | Constants.OrderByDirection.Ascending -> false
            | _ -> true

        let sort = 
            match searchQuery.OrderBy with
            | null -> Sort.RELEVANCE
            | _ -> 
                match indexWriter.Settings.Fields.TryGetValue(searchQuery.OrderBy) with
                | (true, field) -> 
                    if field |> FieldSchema.hasDocValues then
                        new Sort(new SortField(field.SchemaName, field.FieldType.SortFieldType, sortOrder))
                    else
                        Sort.RELEVANCE
                | _ -> Sort.RELEVANCE
        
        let distinctBy = 
            if not <| String.IsNullOrWhiteSpace(searchQuery.DistinctBy) then 
                match indexWriter.Settings.Fields.TryGetValue(searchQuery.DistinctBy) with
                | true, field -> 
                    match field |> FieldSchema.isTokenized with
                    | false -> Some(field, new HashSet<string>(StringComparer.OrdinalIgnoreCase))
                    | true -> None
                | _ -> None
            else None
        
        let count = 
            match searchQuery.Count with
            | 0 -> 10 + searchQuery.Skip
            | _ -> searchQuery.Count + searchQuery.Skip
        
        let searchShard(x : ShardWriter) =
            // This is to enable proper sorting
            let topFieldCollector = 
                TopFieldCollector.Create(sort, count, null, true, true, true)
            indexSearchers.[x.ShardNo].IndexSearcher.Search(query, topFieldCollector)
            topDocsCollection.[x.ShardNo] <- topFieldCollector.TopDocs()
            
        indexWriter.ShardWriters |> Array.Parallel.iter searchShard
        let totalDocs = TopDocs.Merge(sort, count, topDocsCollection)
        let hits = totalDocs.ScoreDocs
        let recordsReturned = totalDocs.ScoreDocs.Count() - searchQuery.Skip
        let totalAvailable = totalDocs.TotalHits
        
        let cutOff = 
            match searchQuery.CutOff with
            | 0.0 -> None
            | cutOffValue -> Some(float32 <| cutOffValue, totalDocs.GetMaxScore())
        
        let highlighterOptions = 
            if notNull searchQuery.Highlights then 
                match searchQuery.Highlights.HighlightedFields with
                | x when x.Length = 1 -> 
                    match indexWriter.Settings.Fields.TryGetValue(x.First()) with
                    | (true, field) -> 
                        let htmlFormatter = 
                            new SimpleHTMLFormatter(searchQuery.Highlights.PreTag, searchQuery.Highlights.PostTag)
                        Some(field, new Highlighter(htmlFormatter, new QueryScorer(query)))
                    | _ -> None
                | _ -> None
            else None
        
        let inline getHighlighter (document : LuceneDocument, shardIndex, doc) = 
            if highlighterOptions.IsSome then 
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
                    frags
                    |> Array.filter (fun frag -> notNull (frag) && frag.GetScore() > float32 (0.0))
                    |> Array.map (fun frag -> frag.ToString())
                else Array.empty<string>
            else Array.empty<string>
        
        let processDocument (hit : ScoreDoc, document : LuceneDocument) = 
            let timeStamp = 
                let t = document.Get(indexWriter.GetSchemaName(TimeStampField.Name))
                if isNull t then
                    0L
                else
                    int64 t
            let fields = getDocument (indexWriter, searchQuery, document)
            
            let resultDoc = new Model.Document()
            resultDoc.Id <- document.Get(indexWriter.GetSchemaName(IdField.Name))
            resultDoc.IndexName <- indexWriter.Settings.IndexName
            resultDoc.TimeStamp <- timeStamp
            resultDoc.Fields <- fields
            resultDoc.Score <- if searchQuery.ReturnScore then float (hit.Score)
                               else 0.0
            resultDoc.Highlights <- getHighlighter (document, hit.ShardIndex, hit.Doc)
            resultDoc
        
        let distinctByFilter (document : LuceneDocument) = 
            match distinctBy with
            | Some(field, hashSet) -> 
                let distinctByValue = document.Get(indexWriter.GetSchemaName(field.FieldName))
                if notNull distinctByValue && hashSet.Add(distinctByValue) then Some(document)
                else None
            | None -> Some(document)
        
        let cutOffFilter (hit : ScoreDoc) (document : LuceneDocument option) = 
            match cutOff with
            | Some(cutOffValue, maxScore) -> 
                if (hit.Score / maxScore * 100.0f >= cutOffValue) then document
                else None
            | None -> document
        
        // Start composing the seach results
        let results = 
            seq { 
                for i = searchQuery.Skip to hits.Length - 1 do
                    let hit = hits.[i]
                    let document = indexSearchers.[hit.ShardIndex].IndexSearcher.Doc(hit.Doc)
                    let result = distinctByFilter document |> cutOffFilter hit
                    match result with
                    | Some(doc) -> yield processDocument (hit, document)
                    | None -> ()
                // Dispose the searchers
                for i in 0..indexSearchers.Length - 1 do
                    (indexSearchers.[i] :> IDisposable).Dispose()
            }
        
        new SearchResults(RecordsReturned = recordsReturned,
                          BestScore = totalDocs.GetMaxScore(),
                          TotalAvailable = totalAvailable,
                          Documents = results.ToArray())