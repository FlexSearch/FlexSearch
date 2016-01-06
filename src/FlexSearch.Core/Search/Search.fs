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

/// FlexQuery interface     
type IFlexQuery = 
    abstract QueryName : unit -> string []
    abstract GetQuery : Field.T * string [] * Dictionary<string, string> option -> Result<Query>

/// Interface for implementing query functions.
/// Query functions can be of two types: constant and variable.
///
/// Constant query functions don't have any field names in them. They ultimately 
/// compute to a constant value.
///
/// In variable query functions, the first parameter is a field name. Any other parameter
/// should be a constant value. Variable query functions modify the given SearchQuery so 
/// that it mimics the intended function. They don't return a constant value.
type IFlexQueryFunction = 
    abstract GetConstantResult : Constant list * Dictionary<string, IFlexQueryFunction> * Dictionary<string, string> option -> Result<string option>
    abstract GetVariableResult : Field.T * FieldFunction * IFlexQuery * string [] option * Dictionary<string, string> option * Dictionary<string, IFlexQueryFunction> -> Result<Query>

// ----------------------------------------------------------------------------
// Contains all predefined flex queries. Also contains the search factory service.
// The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
[<AutoOpen>]
module SearchDsl = 
    let inline queryNotFound queryName = QueryNotFound <| queryName
    let inline fieldNotFound fieldName = InvalidFieldName <| fieldName

    let getQueryFunction (functions : Dictionary<string, IFlexQueryFunction>) funcName =
        match functions.TryGetValue(funcName) with
        | true, func -> ok func
        | _ -> fail <| FunctionNotFound funcName

    let handleFunctionValue fName 
                            parameters 
                            (queryFunctionTypes : Dictionary<string, IFlexQueryFunction>) 
                            (source : Dictionary<string,string> option) =
        fName |> getQueryFunction queryFunctionTypes
        >>= fun func ->
                match func.GetConstantResult(parameters, queryFunctionTypes, source) with
                | Ok(r) -> ok <| r
                | Fail(e) -> fail e

    // Gets a field from the given search profile.
    // Empty/blank values are considered an error
    let getFieldFromSource (source : Dictionary<string, string>) fieldName =
            match source.TryGetValue(fieldName) with
            | true, v1 -> 
                if isNotBlank v1 then ok <| v1
                else fail <| MissingFieldValue fieldName
            | _ -> fail <| MissingFieldValue fieldName

    let validateSearchField (fields : IReadOnlyDictionary<string, Field.T>)  fieldName =
            maybe {
                let! field = fields |> keyExists2 (fieldName, fieldNotFound)
                do! FieldType.searchable field.FieldType |> boolToResult (StoredFieldCannotBeSearched(field.FieldName))
                return field
            }

    let generateQuery (fields : IReadOnlyDictionary<string, Field.T>, predicate : Predicate, 
                       searchQuery : SearchQuery, isProfileBased : Dictionary<string, string> option, 
                       queryTypes : Dictionary<string, IFlexQuery>,
                       queryFunctionTypes : Dictionary<string, IFlexQueryFunction>) = 
        assert (queryTypes.Count > 0)
        assert (queryFunctionTypes.Count > 0)
        let generateMatchAllQuery = ref false
        
        let computeFieldValueAsArray (fieldName : string) (v : Constant) = 
            match v with
            | SingleValue(v) -> 
                if String.IsNullOrWhiteSpace(v) then fail <| MissingFieldValue(fieldName)
                else ok <| [| v |]
            | ValueList(v) -> 
                if v.Length = 0 then fail <| MissingFieldValue(fieldName)
                else ok <| v.ToArray()
            | Constant.Function(name,prms) -> 
                handleFunctionValue name (prms |> Seq.toList) queryFunctionTypes None 
                >>= (fun x -> match x with 
                              | Some(value) -> ok [| value |]
                              | None -> fail <| ValueCouldntBeRetrieved(fieldName))
            | SearchProfileField(n) -> fail <| FieldNamesNotSupportedOutsideSearchProfile("N/A", n)
        
        let computeConstant (fieldName, v : Constant) = 
            match isProfileBased with
            | Some(source) -> 
                match v with
                | SingleValue(v1) -> ok [| v1 |]
                | SearchProfileField(spfn) -> 
                    spfn |> getFieldFromSource source 
                    >>= fun x -> ok [| x |] 
                | Constant.Function(funcName,parameters) -> 
                    handleFunctionValue funcName (parameters |> Seq.toList) queryFunctionTypes (Some(source))
                    >>= (fun result -> match result with 
                                       | Some(value) -> ok [| value |]
                                       // If None is returned and we're in a Search Profile search, then
                                       // we can ignore this condition, thus generate a matchall query
                                       | None -> generateMatchAllQuery := true; ok [||])
                | _ -> fail <| SearchProfileUnsupportedFieldValue(fieldName)
            | None -> v |> computeFieldValueAsArray fieldName
        
        let queryGenerationWrapper fieldName operator constant parameters 
                                   (queryGetter : Field.T -> IFlexQuery -> string[] option -> Result<Query>) =
            maybe {
                    // First validate the field
                    let! field = fieldName |> validateSearchField fields
                    // Then get the type of query operator being used. We might not have
                    // an operator if there is no RHS value
                    let! query = match constant with
                                 | Some(_) -> queryTypes |> keyExists (operator, queryNotFound)
                                 | None -> ok Unchecked.defaultof<IFlexQuery>
                    // Then calculate the value of the constant expression on the RHS of the query,
                    // if we have such a value (see FuncCondition)
                    let! computedConstant = match constant with
                                            | Some(c) -> computeConstant (fieldName, c) >>= (Some >> ok)
                                            | None -> ok None
                    
                    if generateMatchAllQuery.Value = true then return! ok <| getMatchAllDocsQuery()
                    else 
                        let! q = queryGetter field query computedConstant

                        // Apply boost if given
                        q.SetBoost(float32 <| doubleFromOptDict "boost" 1.0 parameters)
                        return q
                }

        /// Generate the query from the CONSTANT condition
        /// This happens when the LHS of the query only has a field and no functions
        let getConstantCondition (fieldName, operator, constant : Constant, p) = 
            fun field (query : IFlexQuery) (computedConstant : string [] option) -> 
                query.GetQuery(field, computedConstant.Value.ToArray(), p)
            |> queryGenerationWrapper fieldName operator (Some constant) p
            
        /// Generate the query from the VARIABLE condition
        /// This happens when the LHS of the query has a function
        let getVariableCondition (fieldFunction : FieldFunction) operator constant p =
            match fieldFunction with 
            | FieldFunction(funcName, fieldName, prms) ->
                let queryGetter field query computedConstant =
                    maybe {
                        // Get the appropriate LHS query function
                        let! fieldQueryFunc = funcName |> getQueryFunction queryFunctionTypes
                        // Compute the final query by analyzing the variable, operator and constant
                        return! fieldQueryFunc.GetVariableResult(field, fieldFunction, query, computedConstant, p, queryFunctionTypes)
                    }

                queryGetter
                |> queryGenerationWrapper fieldName operator constant p

        /// Main rec function responsible for generating predicate
        let rec generateQuery (pred : Predicate) = 
            maybe { 
                match pred with
                | NotPredicate(pr) -> let! notQuery = generateQuery (pr)
                                      return getBooleanQuery() |> addMustNotClause notQuery :> Query
                | Condition(var, o, cnst, p) -> 
                    match var with
                    | Field(f) -> return! getConstantCondition (f, o, cnst, p)
                    | Function(f) -> return! getVariableCondition f o (Some cnst) p
                | FuncCondition(ff) -> return! getVariableCondition ff null None None
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
    
    /// Returns a document from the index
    let getDocument (indexWriter : IndexWriter.T, search : SearchQuery, document : LuceneDocument) = 
        let fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        let getValue(field: Field.T) =
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
                if [MetaFields.IdField; MetaFields.LastModifiedField; MetaFields.ModifyIndex; MetaFields.State]
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
    
    let search (indexWriter : IndexWriter.T, query : Query, searchQuery : SearchQuery) = 
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
                    if field.GenerateDocValue then
                        new Sort(new SortField(field.SchemaName, FieldType.sortField field.FieldType, sortOrder))
                    else
                        Sort.RELEVANCE
                | _ -> Sort.RELEVANCE
        
        let distinctBy = 
            if not <| String.IsNullOrWhiteSpace(searchQuery.DistinctBy) then 
                match indexWriter.Settings.Fields.TryGetValue(searchQuery.DistinctBy) with
                | true, field -> 
                    match field.FieldType with
                    | FieldType.ExactText(_) -> Some(field, new HashSet<string>(StringComparer.OrdinalIgnoreCase))
                    | _ -> None
                | _ -> None
            else None
        
        let count = 
            match searchQuery.Count with
            | 0 -> 10 + searchQuery.Skip
            | _ -> searchQuery.Count + searchQuery.Skip
        
        let searchShard(x : ShardWriter.T) =
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
                let t = document.Get(indexWriter.GetSchemaName(MetaFields.LastModifiedField))
                if isNull t then
                    0L
                else
                    int64 t
            let fields = getDocument (indexWriter, searchQuery, document)
            
            let resultDoc = new Model.Document()
            resultDoc.Id <- document.Get(indexWriter.GetSchemaName(MetaFields.IdField))
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