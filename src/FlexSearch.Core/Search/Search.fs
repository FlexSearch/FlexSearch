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

type Variables = Dictionary<string, string>

/// Signifies the behaviour of the clause when no value
/// is provided for searching
type ClauseMatchRule = 
    /// Default behaviour that is to thrown error when no
    /// value is provided for searching
    | ThrowError = 0
    /// Convert the clause to a match all clause which allows
    /// matching against all values
    | All = 1
    /// Convert the clause to a match none clause which matches
    /// no values
    | None = 2
    /// Use the default value for the field for searching
    | FieldDefault = 3
    /// Use the user defined value to search
    | UseDefault = 4

type ClauseProperties = 
    { MatchRule : ClauseMatchRule
      Filter : bool
      DefaultValue : string option
      Boost : float32 option
      ConstantScore : float32 option
      Switches : IEnumerable<Switch> }
    static member Create(parameters : FunctionParameter list) = 
        let mutable matchRule = ClauseMatchRule.ThrowError
        let mutable defaultValue = None
        let mutable boost = None
        let mutable constantScore = None
        let mutable filter = false
        let switches = new List<Switch>()
        // Could use List.choose here but it is a lot slower
        for p in parameters do
            match p with
            | Switch(s) -> 
                match s.Name with
                | InvariantEqual "MatchAll" -> matchRule <- ClauseMatchRule.All
                | InvariantEqual "MatchNone" -> matchRule <- ClauseMatchRule.None
                | InvariantEqual "MatchFieldDefault" -> matchRule <- ClauseMatchRule.FieldDefault
                | InvariantEqual "UseDefault" -> 
                    match s.Value with
                    | Some v -> 
                        if isBlank v then matchRule <- ClauseMatchRule.FieldDefault
                        else 
                            matchRule <- ClauseMatchRule.UseDefault
                            defaultValue <- Some v
                    | None -> matchRule <- ClauseMatchRule.FieldDefault
                | InvariantEqual "Boost" -> 
                    match s.Value with
                    | Some v -> 
                        if isNotBlank v then 
                            let boostValue = pFloat 1.0f v
                            if boostValue <> 1.0f then boost <- Some boostValue
                    | None -> ()
                | InvariantEqual "ConstantScore" -> 
                    match s.Value with
                    | Some v -> 
                        if isNotBlank v then 
                            let constantValue = pFloat 1.0f v
                            if constantValue <> 1.0f then constantScore <- Some constantValue
                    | None -> ()
                | InvariantEqual "Filter" -> filter <- true
                | _ -> switches.Add(s)
            | _ -> ()
        { MatchRule = matchRule
          Filter = filter
          DefaultValue = defaultValue
          Boost = boost
          ConstantScore = constantScore
          Switches = switches }

/// Used to represent a search operator like allOf etc.
[<Interface>]
type IQueryFunction = 
    abstract GetQuery : FieldSchema * values:IEnumerable<string> * properties:ClauseProperties -> Result<Query>

type SearchBaggage = 
    { Fields : IReadOnlyDictionary<string, FieldSchema>
      QueryFunctions : Dictionary<string, IQueryFunction> }

// ----------------------------------------------------------------------------
// Contains all predefined flex queries. Also contains the search factory service.
// The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
[<AutoOpen>]
module SearchDsl = 
    let ignoreFunctionHandlerName = "isblank"
    let inline queryNotFound queryName = QueryNotFound <| queryName
    let inline fieldNotFound fieldName = InvalidFieldName <| fieldName
    
    let getFieldSchema fieldName (fields : IReadOnlyDictionary<string, FieldSchema>) = 
        maybe { 
            let! field = fields |> keyExists2 (fieldName, fieldNotFound)
            do! FieldSchema.isSearchable field |> boolToResult (StoredFieldCannotBeSearched(field.FieldName))
            return field
        }
    
    // Gets a variable value from the search query.
    // Empty/blank values are considered an error
    let getVariable (name : VariableName) (variables : Variables) = 
        match variables.TryGetValue <| name.ToLower() with
        | true, value -> 
            if isNotBlank value then ok value
            else fail <| MissingVariableValue name
        | _ -> fail <| MissingVariableValue name
    
    /// Checks if there is a need to generate query or not? In case there are no tokens then
    /// the main query could be replaced with match all or match none queries.
    /// Also if the match rule is set to use defaults then we can inject the default value.
    let byPassMainQueryGeneration (fieldName, fieldSchema : FieldSchema, values : List<string>, 
                                   properties : ClauseProperties) = 
        if values.Count = 0 then 
            // Check if there is a configured behaviour for the missing value
            match properties.MatchRule with
            | ClauseMatchRule.All -> 
                getMatchAllDocsQuery()
                |> Some
                |> Ok
            | ClauseMatchRule.None -> 
                getMatchNoDocsQuery()
                |> Some
                |> Ok
            | ClauseMatchRule.FieldDefault -> 
                values.Add(fieldSchema.FieldType.DefaultStringValue)
                None |> Ok
            | ClauseMatchRule.UseDefault -> 
                values.Add(properties.DefaultValue.Value)
                None |> Ok
            | _ -> fail <| MissingVariableValue(fieldName)
        else None |> Ok
    
    /// Generate the leaf node clause for a given Clause predicate 
    let getClause (funcName, fieldName, parameters : FunctionParameter list) (searchQuery : SearchQuery) 
        (baggage : SearchBaggage) = 
        maybe { 
            let! fieldSchema = baggage.Fields |> getFieldSchema fieldName
            let! func = baggage.QueryFunctions |> keyExists2 (funcName, queryNotFound)
            let properties = ClauseProperties.Create(parameters)
            let tokens = stringListPool.Get()
            for p in parameters do
                match p with
                | Constant(c) -> fieldSchema |> FieldSchema.getTokens (c, tokens)
                | Variable(v) -> 
                    match searchQuery.Variables.TryGetValue <| v.ToLower() with
                    | true, value -> 
                        if isNotBlank value then fieldSchema |> FieldSchema.getTokens (value, tokens)
                    | _ -> ()
                | _ -> ()
            // We can bypass main query if we don't have any tokens to search
            let! byPassMainQuery = byPassMainQueryGeneration (fieldName, fieldSchema, tokens, properties)
            match byPassMainQuery with
            | Some(q) -> 
                // Make sure to return the item to the pool
                stringListPool.Return(tokens)
                return q
            | None -> 
                let mutable q = func.GetQuery(fieldSchema, tokens, properties)
                // It is safe to return the item to the pool as we have used
                // it in query generation.
                stringListPool.Return(tokens)
                match q with
                | Ok(q') -> 
                    let mutable q'' = q'
                    if properties.Boost.IsSome then q'' <- getBoostQuery (q', properties.Boost.Value)
                    if properties.ConstantScore.IsSome then 
                        q'' <- getConstantScoreQuery (q'', properties.ConstantScore.Value)
                    return q''
                | _ -> return! q
        }
    
    let rec generateQuery (predicate : Predicate) (searchQuery : SearchQuery) (baggage : SearchBaggage) = 
        maybe { 
            match predicate with
            | NotPredicate(pr) -> 
                let! notQuery = generateQuery pr searchQuery baggage
                return getBooleanQuery()
                       |> addMustNotClause notQuery
                       |> addMatchAllClause :> Query
            | Clause(funcName, fieldName, parameters) -> 
                return! getClause (funcName, fieldName, parameters) searchQuery baggage
            | OrPredidate(lhs, rhs) -> 
                let! lhsQuery = generateQuery lhs searchQuery baggage
                let! rhsQuery = generateQuery rhs searchQuery baggage
                return getBooleanQuery()
                       |> addShouldClause lhsQuery
                       |> addShouldClause rhsQuery :> Query
            | AndPredidate(lhs, rhs) -> 
                let! lhsQuery = generateQuery lhs searchQuery baggage
                let! rhsQuery = generateQuery rhs searchQuery baggage
                return getBooleanQuery()
                       |> addMustClause lhsQuery
                       |> addMustClause rhsQuery :> Query
        }
    
    /// Returns a document from the index
    let getDocument (indexWriter : IndexWriter, search : SearchQuery, document : LuceneDocument) = 
        let fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        
        let getValue (field : FieldSchema) = 
            let value = document.Get(field.SchemaName)
            if notNull value then 
                if value = Constants.StringDefaultValue && search.ReturnEmptyStringForNull then 
                    fields.Add(field.FieldName, String.Empty)
                else fields.Add(field.FieldName, value)
        match search.Columns with
        // Return no other columns when nothing is passed
        | _ when search.Columns.Length = 0 -> ()
        // Return all columns when *
        | _ when search.Columns.First() = "*" -> 
            for field in indexWriter.Settings.Fields do
                if [ IdField.Name; TimeStampField.Name; ModifyIndexField.Name; StateField.Name ] 
                   |> Seq.contains field.FieldName then ()
                else getValue (field)
        // Return only the requested columns
        | _ -> 
            for fieldName in search.Columns do
                match indexWriter.Settings.Fields.TryGetValue(fieldName) with
                | (true, field) -> getValue (field)
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
                    else Sort.RELEVANCE
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
        
        let searchShard (x : ShardWriter) = 
            // This is to enable proper sorting
            let topFieldCollector = TopFieldCollector.Create(sort, count, null, true, true, true)
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
                if isNull t then 0L
                else int64 t
            
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
        
        new SearchResults(RecordsReturned = recordsReturned, BestScore = totalDocs.GetMaxScore(), 
                          TotalAvailable = totalAvailable, Documents = results.ToArray())
