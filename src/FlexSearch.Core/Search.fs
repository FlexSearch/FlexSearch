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
    abstract GetQuery : Field.T * string [] * Dictionary<string, string> option -> Result<Query>

/// Interface for implementing query functions
type IFlexQueryFunction = 
    abstract GetResult : Value list * Dictionary<string, IFlexQueryFunction> * Dictionary<string, string> option -> Result<string>

[<AutoOpenAttribute>]
module SearchResultComponents = 
    /// Represents the search result format supported
    /// by the engine
    type T = 
        | StructuredResult of Document
        | FlatResult of Dictionary<string, string>
    
    /// Represents the search related meta data that can
    /// be exposed through a search result. This can be
    /// easily extended in future to include more things.
    type ResultMeta = 
        { RecordsReturned : int
          BestScore : float32
          TotalAvailable : int }
    
    /// Wrapper around the search result. This is also used
    /// to transform one result type to another
    type SearchResults<'T> = 
        { Meta : ResultMeta
          Documents : seq<'T> }
    
    /// Transforms a SearchResult to StructuredResult 
    let toStructuredResult (result : T) = 
        match result with
        | StructuredResult(doc) -> doc
        | _ -> failwithf "Internal error: Expecting Structured result."
    
    /// Transforms a SearchResult to FlatResult
    let toFlatResult (result : T) = 
        match result with
        | FlatResult(doc) -> doc
        | _ -> failwithf "Internal error: Expecting Flat result."
    
    /// Transforms a result seq to a Document seq
    let toStructuredResults (result : SearchResults<T>) = 
        let docs = 
            seq { 
                for doc in result.Documents do
                    match doc with
                    | StructuredResult(d) -> yield d
                    | FlatResult(_) -> failwithf "Internal error: Should have never returned a Flat Document."
            }
        { Meta = result.Meta
          Documents = docs }
    
    /// Transforms a result seq to a Flat Document seq
    let toFlatResults (result : SearchResults<T>) = 
        let docs = 
            seq { 
                for doc in result.Documents do
                    match doc with
                    | StructuredResult(_) -> failwithf "Internal error: Should have never returned a Result Document."
                    | FlatResult(d) -> yield d
            }
        { Meta = result.Meta
          Documents = docs }
    
    /// Transforms a result seq to Search results
    let toSearchResults (result : SearchResults<T>) = 
        let searchResults = new SearchResults()
        searchResults.RecordsReturned <- result.Meta.RecordsReturned
        searchResults.TotalAvailable <- result.Meta.TotalAvailable
        searchResults.Documents <- toStructuredResults(result).Documents.ToList()
        searchResults

// ----------------------------------------------------------------------------
// Contains all predefined flex queries. Also contains the search factory service.
// The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
[<AutoOpen>]
module SearchDsl = 
    let inline queryNotFound queryName = QueryNotFound <| queryName
    let inline fieldNotFound fieldName = InvalidFieldName <| fieldName

    let handleFunctionValue 
        fName 
        parameters 
        (queryFunctionTypes : Dictionary<string, IFlexQueryFunction>) 
        (source : Dictionary<string,string> option) =
            match queryFunctionTypes.TryGetValue(fName) with
            | true, func -> 
                match func.GetResult(parameters, queryFunctionTypes, source) with
                | Ok(r) -> ok <| r
                | Fail(e) -> fail e
            | _ -> fail <| FunctionNotFound (sprintf "%A" <| Func(fName, parameters))

    let generateQuery (fields : IReadOnlyDictionary<string, Field.T>, predicate : Predicate, 
                       searchQuery : SearchQuery, isProfileBased : Dictionary<string, string> option, 
                       queryTypes : Dictionary<string, IFlexQuery>,
                       queryFunctionTypes : Dictionary<string, IFlexQueryFunction>) = 
        assert (queryTypes.Count > 0)
        let generateMatchAllQuery = ref false
        
        let getFieldValueAsArray (fieldName : string) (v : Value) = 
            match v with
            | SingleValue(v) -> 
                if String.IsNullOrWhiteSpace(v) then fail <| MissingFieldValue(fieldName)
                else ok <| [| v |]
            | ValueList(v) -> 
                if v.Length = 0 then fail <| MissingFieldValue(fieldName)
                else ok <| v.ToArray()
            | Func(name,prms) -> handleFunctionValue name (prms |> Seq.toList) queryFunctionTypes None 
                                 >>= (fun x -> ok [| x |])
            | FieldName(n) -> fail <| FieldNamesNotSupportedOutsideSearchProfile("N/A", n)
        
        let getValueForSearchProfile (fieldName, v : string, source : Dictionary<string, string>) = 
            generateMatchAllQuery.Value <- false
            match v with
            // Match self but fail if value is not found
            | "" | "[!]" -> 
                match source.TryGetValue(fieldName) with
                | true, v1 -> 
                    if isNotBlank v1 then ok <| [| v1 |]
                    else fail <| MissingFieldValue fieldName
                | _ -> fail <| MissingFieldValue fieldName
            // Match self but ignore clause if value is not found
            | "[*]" -> 
                match source.TryGetValue(fieldName) with
                | true, v1 -> 
                    if isNotBlank v1 then ok <| [| v1 |]
                    else 
                        generateMatchAllQuery.Value <- true
                        ok <| Array.empty
                | _ -> 
                    generateMatchAllQuery.Value <- true
                    ok <| Array.empty
            // Match self but use a default value if value is not found
            | x when x.StartsWith("[") && x.EndsWith("]") -> 
                match source.TryGetValue(fieldName) with
                | true, v1 -> 
                    if isNotBlank v1 then ok <| [| v1 |]
                    else ok <| [| x |> between '[' ']' |]
                | _ -> ok <| [| x |> between '[' ']' |]
            // Cross matching cases
            // Default cross matching case
            | x when x.StartsWith("<") && (x.EndsWith(">") || x.EndsWith(">[!]")) -> 
                match source.TryGetValue(x |> between '<' '>') with
                | true, v1 -> 
                    if isNotBlank v1 then ok <| [| v1 |]
                    else fail <| MissingFieldValue fieldName
                | _ -> fail <| MissingFieldValue fieldName
            // Cross matching and ignore clause if value is not found
            | x when x.StartsWith("<") && x.EndsWith(">[*]") -> 
                match source.TryGetValue(x |> between '<' '>') with
                | true, v1 -> 
                    if isNotBlank v1 then ok <| [| v1 |]
                    else 
                        generateMatchAllQuery.Value <- true
                        ok <| Array.empty
                | _ -> 
                    generateMatchAllQuery.Value <- true
                    ok <| Array.empty
            // Cross matching and use default if value is not found
            | x when x.StartsWith("<") && x.EndsWith("]") -> 
                match source.TryGetValue(x |> between '<' '>') with
                | true, v1 -> 
                    if isNotBlank v1 then ok <| [| v1 |]
                    else ok <| [| x |> between '[' ']' |]
                | _ -> ok <| [| x |> between '[' ']' |]
            // Constant value
            | x -> ok [| x |]
        
        let getValue (fieldName, v : Value) = 
            match isProfileBased with
            | Some(source) -> 
                match v with
                | SingleValue(v1) -> getValueForSearchProfile (fieldName, v1, source)
                | Func(funcName,parameters) -> 
                    handleFunctionValue funcName (parameters |> Seq.toList) queryFunctionTypes (Some(source))
                    >>= (fun result -> ok [| result |])
                | _ -> fail <| SearchProfileUnsupportedFieldValue(fieldName)
            | None -> v |> getFieldValueAsArray fieldName
        
        /// Generate the query from the condition
        let getCondition (fieldName, operator, v : Value, p) = 
            maybe { 
                let! field = fields |> keyExists2 (fieldName, fieldNotFound)
                do! FieldType.searchable field.FieldType |> boolToResult (StoredFieldCannotBeSearched(field.FieldName))
                let! query = queryTypes |> keyExists (operator, queryNotFound)
                let! value = getValue (fieldName, v)
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
                if field.FieldName = Constants.IdField || field.FieldName = Constants.LastModifiedField then ()
                else getValue(field)
        // Return only the requested columns
        | _ -> 
            for fieldName in search.Columns do
                match indexWriter.Settings.FieldsLookup.TryGetValue(fieldName) with
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
            | InvariantEqual "asc" -> false
            | _ -> true

        let sort = 
            match searchQuery.OrderBy with
            | null -> Sort.RELEVANCE
            | _ -> 
                match indexWriter.Settings.FieldsLookup.TryGetValue(searchQuery.OrderBy) with
                | (true, field) -> 
                    if field.GenerateDocValue then
                        new Sort(new SortField(field.SchemaName, FieldType.sortField field.FieldType, sortOrder))
                    else
                        Sort.RELEVANCE
                | _ -> Sort.RELEVANCE
        
        let distinctBy = 
            if not <| String.IsNullOrWhiteSpace(searchQuery.DistinctBy) then 
                match indexWriter.Settings.FieldsLookup.TryGetValue(searchQuery.DistinctBy) with
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
                    match indexWriter.Settings.FieldsLookup.TryGetValue(x.First()) with
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
            let timeStamp = int64 (document.Get(indexWriter.GetSchemaName(Constants.LastModifiedField)))
            let fields = getDocument (indexWriter, searchQuery, document)
            if searchQuery.ReturnFlatResult then 
                fields.[Constants.IdField] <- document.Get(indexWriter.GetSchemaName(Constants.IdField))
                fields.[Constants.LastModifiedField] <- document.Get(indexWriter.GetSchemaName(Constants.LastModifiedField))
                if searchQuery.ReturnScore then fields.[Constants.Score] <- hit.Score.ToString()
                SearchResultComponents.FlatResult(fields)
            else 
                let resultDoc = new Document()
                resultDoc.Id <- document.Get(indexWriter.GetSchemaName(Constants.IdField))
                resultDoc.IndexName <- indexWriter.Settings.IndexName
                resultDoc.TimeStamp <- timeStamp
                resultDoc.Fields <- fields
                resultDoc.Score <- if searchQuery.ReturnScore then float (hit.Score)
                                   else 0.0
                resultDoc.Highlights <- getHighlighter (document, hit.ShardIndex, hit.Doc)
                SearchResultComponents.StructuredResult(resultDoc)
        
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
        
        { Meta = 
              { RecordsReturned = recordsReturned
                BestScore = totalDocs.GetMaxScore() 
                TotalAvailable = totalAvailable }
          Documents = results }

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
                zeroOneOrManyQuery <| getTerms (flexIndexField, values) <| getTermQuery flexIndexField.SchemaName 
                <| getBooleanClause parameters

/// Fuzzy Query
[<Name("fuzzy_match"); Sealed>]
type FlexFuzzyQuery() = 
    interface IFlexQuery with
        member __.QueryName() = [| "fuzzy"; "~=" |]
        member __.GetQuery(flexIndexField, values, parameters) = 
            let slop = parameters |> intFromOptDict "slop" 1
            let prefixLength = parameters |> intFromOptDict "prefixlength" 0
            zeroOneOrManyQuery <| getTerms (flexIndexField, values) 
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
            let terms = getTerms (flexIndexField, values)
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

[<AutoOpen>]
module QueryFunctionHelpers = 
    // Convert the parameters to the integers.
    // We aren't expecting arrays
    let rec convertToInts typeName source queryFunctionTypes parameters =          
        match parameters with
        | [] -> ok []
        | SingleValue(v) :: rest -> convertToInts typeName source queryFunctionTypes rest
                                    >>= (fun vs -> v :: vs |> ok)
        | ValueList(_) :: rest -> 
            fail <| FunctionParamTypeMismatch(
                typeName,
                "Single values, functions or fieldnames",
                "Value list")
        | Func(n,ps) :: rest -> 
            convertToInts typeName source queryFunctionTypes rest >>= (fun vs -> 
                handleFunctionValue n (ps |> Seq.toList) queryFunctionTypes source
                >>= (fun v -> v :: vs |> ok))
        | FieldName(name) :: rest ->
            match source with
            | Some(sourceDict) -> match sourceDict.TryGetValue(name) with
                                    | true, v -> convertToInts typeName source queryFunctionTypes rest
                                                >>= (fun vs -> v :: vs |> ok)
                                    | _ -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
            | None -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)

    // Convert the single parameter to a string
    let getSingleStringParam typeName source queryFunctionTypes parameters =
        match parameters with
        | [] -> ok ""
        | [SingleValue(v)] -> ok v
        | [ValueList(_)] -> fail <| FunctionParamTypeMismatch(
                                typeName,
                                "Single values, functions or fieldnames",
                                "Value list")
        | [Func(n,ps)] -> handleFunctionValue n (ps |> Seq.toList) queryFunctionTypes source
                          >>= ok
        | [FieldName(name)] ->
            match source with
            | Some(sourceDict) -> match sourceDict.TryGetValue(name) with
                                    | true, v -> ok v
                                    | _ -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
            | None -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
        | _ -> fail <| NumberOfFunctionParametersMismatch(typeName, 1, parameters.Length)

    let getStringParam typeName source queryFunctionTypes parameters accumulatedResult =  
        match parameters with
        | [] -> fail <| NotEnoughParameters typeName
        | SingleValue(v) :: rest -> ok (v :: accumulatedResult, rest)
        | ValueList(_) :: rest -> fail <| FunctionParamTypeMismatch(
                                    typeName,
                                    "Single values, functions or fieldnames",
                                    "Value list")
        | Func(n,ps) :: rest -> handleFunctionValue n (ps |> Seq.toList) queryFunctionTypes source
                                >>= fun result -> ok (result :: accumulatedResult,rest)
        | FieldName(name) :: rest ->
            match source with
            | Some(sourceDict) -> match sourceDict.TryGetValue(name) with
                                    | true, v -> ok (v :: accumulatedResult, rest)
                                    | _ -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
            | None -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)

    // Applies the given folding function to the list of integers, after processing them 
    // from a list of function parameter types
    let doForParameters parameters typeName source queryFunctionTypes doFunction =
        parameters
        |> convertToInts typeName source queryFunctionTypes
        >>= (fun stringParams -> 
            stringParams
            |> Seq.map Convert.ToDouble
            // Calculate the sum
            |> doFunction
            // Bring back to string type
            |> (fun x -> x.ToString()) |> ok)

    // Applies the given function to the string value, after processing it from a list of 
    // of function parameter types
    let doForSingleParameter parameters typeName source queryFunctionTypes doFunction =
        parameters
        |> getSingleStringParam typeName source queryFunctionTypes
        >>= (doFunction >> ok)

// ----------------------------------------------------------------------------
// Functions in queries
// These functions are executed by the FlexSearch server before it goes to 
// Lucene search.
// ---------------------------------------------------------------------------- 
[<Name("add"); Sealed>]
type AddFunc() =
    interface IFlexQueryFunction with
        member __.GetResult(parameters, queryFunctionTypes, source) = 
            try
                Seq.sum
                |> doForParameters parameters
                                   (__.GetType() |> getTypeNameFromAttribute) 
                                   source 
                                   queryFunctionTypes 
                                   
            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)

[<Name("multiply"); Sealed>]
type MultiplyFunc() =
    interface IFlexQueryFunction with
        member __.GetResult(parameters, queryFunctionTypes, source) = 
            try
                Seq.fold (*) 1.0
                |> doForParameters parameters
                                   (__.GetType() |> getTypeNameFromAttribute) 
                                   source 
                                   queryFunctionTypes 
            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)

[<Name("max"); Sealed>]
type MaxFunc() =
    interface IFlexQueryFunction with
        member __.GetResult(parameters, queryFunctionTypes, source) = 
            try
                Seq.max
                |> doForParameters parameters
                                   (__.GetType() |> getTypeNameFromAttribute) 
                                   source 
                                   queryFunctionTypes 
            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)

[<Name("min"); Sealed>]
type MinFunc() =
    interface IFlexQueryFunction with
        member __.GetResult(parameters, queryFunctionTypes, source) = 
            try
                Seq.min
                |> doForParameters parameters
                                   (__.GetType() |> getTypeNameFromAttribute) 
                                   source 
                                   queryFunctionTypes 
            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)

[<Name("avg"); Sealed>]
type AvgFunc() =
    interface IFlexQueryFunction with
        member __.GetResult(parameters, queryFunctionTypes, source) = 
            try
                Seq.average
                |> doForParameters parameters
                                   (__.GetType() |> getTypeNameFromAttribute) 
                                   source 
                                   queryFunctionTypes 
            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)

[<Name("len"); Sealed>]
type LenFunc() =
    interface IFlexQueryFunction with
        member __.GetResult(parameters, queryFunctionTypes, source) = 
            try
                String.length >> fun x -> x.ToString()
                |> doForSingleParameter parameters
                                        (__.GetType() |> getTypeNameFromAttribute) 
                                        source 
                                        queryFunctionTypes 
            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)

[<Name("upper"); Sealed>]
type UpperFunc() =
    interface IFlexQueryFunction with
        member __.GetResult(parameters, queryFunctionTypes, source) = 
            try
                fun (x : string) -> x.ToUpper()
                |> doForSingleParameter parameters
                                        (__.GetType() |> getTypeNameFromAttribute) 
                                        source 
                                        queryFunctionTypes 
            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)

[<Name("lower"); Sealed>]
type LowerFunc() =
    interface IFlexQueryFunction with
        member __.GetResult(parameters, queryFunctionTypes, source) = 
            try
                fun (x : string) -> x.ToLower()
                |> doForSingleParameter parameters
                                        (__.GetType() |> getTypeNameFromAttribute) 
                                        source 
                                        queryFunctionTypes 
            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)

[<Name("substr"); Sealed>]
type SubstrFunc() =
    interface IFlexQueryFunction with
        member __.GetResult(parameters, queryFunctionTypes, source) = 
            try
                let typeName = "substr"

                let parsedParams =
                    if parameters.Length <> 3 then 
                        fail <| NumberOfFunctionParametersMismatch(typeName, 3, parameters.Length)
                    else
                        getStringParam typeName source queryFunctionTypes parameters []
                        >>= fun (acc, rest) -> getStringParam typeName source queryFunctionTypes rest acc
                        >>= fun (acc, rest) -> getStringParam typeName source queryFunctionTypes rest acc
                        >>= fun (acc, rest) -> ok acc

                parsedParams
                >>= fun ps -> 
                    match ps |> List.rev with
                    | [inputString; startStr; lengthStr] -> 
                        inputString.Substring(Convert.ToInt32 startStr, Convert.ToInt32 lengthStr) |> ok
                    | _ -> fail <| NumberOfFunctionParametersMismatch(typeName, 3, ps.Length)
            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)