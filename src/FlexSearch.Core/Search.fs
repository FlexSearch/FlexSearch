namespace FlexSearch.Core

open FlexLucene.Analysis
open FlexLucene.Analysis.Core
open FlexLucene.Analysis.Miscellaneous
open FlexLucene.Analysis.Standard
open FlexLucene.Analysis.Tokenattributes
open FlexLucene.Analysis.Util
open FlexLucene.Document
open FlexLucene.Index
open FlexLucene.Queries
open FlexLucene.Queryparser.Classic
open FlexLucene.Queryparser.Flexible
open FlexLucene.Search
open FlexLucene.Search.Highlight
open FlexLucene.Search.Postingshighlight
open FlexSearch.Core
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.Linq
open java.io
open java.util

/// <summary>
/// FlexQuery interface     
/// </summary>
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
    let registeration = new Dictionary<string, IFlexQuery>(StringComparer.OrdinalIgnoreCase)
    let flexCharTermAttribute = 
        lazy java.lang.Class.forName 
                 (typeof<FlexLucene.Analysis.Tokenattributes.CharTermAttribute>.AssemblyQualifiedName)
    
    /// Utility function to get tokens from the search string based upon the passed analyzer
    /// This will enable us to avoid using the Lucene query parser
    /// We cannot use simple white space based token generation as it really depends 
    /// upon the analyzer used
    let inline parseTextUsingAnalyzer (analyzer : FlexLucene.Analysis.Analyzer, fieldName, queryText) = 
        let tokens = new List<string>()
        let source : TokenStream = analyzer.TokenStream(fieldName, new StringReader(queryText))
        // Get the CharTermAttribute from the TokenStream
        let termAtt = source.AddAttribute(flexCharTermAttribute.Value)
        try 
            try 
                source.Reset()
                while source.incrementToken() do
                    tokens.Add(termAtt.ToString())
                source.End()
            with ex -> ()
        finally
            source.Close()
        tokens
    
    let inline IsStoredField(flexField : Field.T) = 
        match flexField.FieldType with
        | FieldType.Stored -> fail (StoredFieldCannotBeSearched(flexField.FieldName))
        | _ -> ok()
    
    let inline processDictionary (parameters : Dictionary<string, string> option, key, parse, defaultValue) = 
        match parameters with
        | Some(p) -> 
            match p.TryGetValue(key) with
            | true, value -> 
                match parse (value) with
                | (true, result) -> result
                | _ -> defaultValue
            | _ -> defaultValue
        | _ -> defaultValue
    
    let inline GetIntValueFromMap (parameters : Dictionary<string, string> option) key defaultValue = 
        processDictionary (parameters, key, (System.Int32.TryParse), defaultValue)
    
    let inline GetStringValueFromMap (parameters : Dictionary<string, string> option) key defaultValue = 
        let parser = 
            fun x -> 
                if String.IsNullOrWhiteSpace(x) then (false, "")
                else (true, x)
        processDictionary (parameters, key, parser, defaultValue)
    
    let inline GetBoolValueFromMap (parameters : Dictionary<string, string> option) key defaultValue = 
        processDictionary (parameters, key, (System.Boolean.TryParse), defaultValue)
    
    let GenerateQuery(fields : Dictionary<string, Field.T>, predicate : Predicate, searchQuery : SearchQuery.T, 
                      isProfileBased : Dictionary<string, string> option, queryTypes : Dictionary<string, IFlexQuery>) = 
        assert (queryTypes.Count > 0)
        let rec generateQuery (pred : Predicate) = 
            maybe { 
                let validateFieldExists (f) = 
                    match fields.TryGetValue(f) with
                    | true, a -> ok (a)
                    | _ -> fail (KeyNotFound(f))
                
                let generateMatchAllQuery = ref false
                match pred with
                | NotPredicate(pr) -> 
                    let! notQuery = generateQuery (pr)
                    let query = new BooleanQuery()
                    query.Add(new BooleanClause(notQuery, BooleanClause.Occur.MUST_NOT))
                    return (query :> Query)
                | Condition(f, o, v, p) -> 
                    let! field = validateFieldExists (f)
                    do! IsStoredField(field)
                    let! query = KeyExists(o, queryTypes)
                    let! value = maybe { 
                                     match isProfileBased with
                                     | Some(source) -> 
                                         match source.TryGetValue(f) with
                                         | true, v' -> return [| v' |]
                                         | _ -> 
                                             match searchQuery.MissingValueConfiguration.TryGetValue(f) with
                                             | true, configuration -> 
                                                 match configuration with
                                                 | MissingValueOption.Default -> return! v.GetValueAsArray()
                                                 | MissingValueOption.ThrowError -> return! fail (MissingFieldValue(f))
                                                 | MissingValueOption.Ignore -> 
                                                     generateMatchAllQuery := true
                                                     return [| "" |]
                                                 | _ -> return! fail (UnknownMissingVauleOption(f))
                                             | _ -> 
                                                 // Check if a non blank value is provided as a part of the query
                                                 return! v.GetValueAsArray()
                                     | None -> return! v.GetValueAsArray()
                                 }
                    if generateMatchAllQuery.Value = true then return! ok (new MatchAllDocsQuery() :> Query)
                    else 
                        let! q = query.GetQuery(field, value.ToArray(), p)
                        match p with
                        | Some(p') -> 
                            match p'.TryGetValue("boost") with
                            | true, b -> 
                                match Int32.TryParse(b) with
                                | true, b' -> q.SetBoost(float32 (b'))
                                | _ -> ()
                            | _ -> ()
                        | None -> ()
                        return q
                | OrPredidate(lhs, rhs) -> 
                    let! lhsQuery = generateQuery (lhs)
                    let! rhsQuery = generateQuery (rhs)
                    let query = new BooleanQuery()
                    query.Add(new BooleanClause(lhsQuery, BooleanClause.Occur.SHOULD))
                    query.Add(new BooleanClause(rhsQuery, BooleanClause.Occur.SHOULD))
                    return query :> Query
                | AndPredidate(lhs, rhs) -> 
                    let! lhsQuery = generateQuery (lhs)
                    let! rhsQuery = generateQuery (rhs)
                    let query = new BooleanQuery()
                    query.Add(new BooleanClause(lhsQuery, BooleanClause.Occur.MUST))
                    query.Add(new BooleanClause(rhsQuery, BooleanClause.Occur.MUST))
                    return query :> Query
            }
        generateQuery predicate
    
    let private Search(indexWriter : IndexWriter.T, query : Query, search : SearchQuery.T) = 
        let indexSearchers = indexWriter |> IndexWriter.getRealTimeSearchers
        // Each thread only works on a separate part of the array and as no parts are shared across
        // multiple threads the below variables are thread safe. The cost of using blocking collection vs. 
        // array per search is high
        let topDocsCollection : TopDocs array = Array.zeroCreate indexSearchers.Length
        
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
    
    let GetDocument(indexWriter : IndexWriter.T, search : SearchQuery.T, document : Document) = 
        let fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        match search.Columns with
        // Return no other columns when nothing is passed
        | x when search.Columns.Length = 0 -> ()
        // Return all columns when *
        | x when search.Columns.First() = "*" -> 
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
    
    let SearchDocumentSeq(indexWriter : IndexWriter.T, query : Query, search : SearchQuery.T) = 
        let (hits, highlighterOptions, recordsReturned, totalAvailable, indexSearchers) = 
            Search(indexWriter, query, search)
        let skipped = ref 0
        
        let results = 
            seq { 
                for hit in hits do
                    if search.Skip > 0 && skipped.Value < search.Skip then skipped.Value <- skipped.Value + 1
                    else 
                        let document = indexSearchers.[hit.ShardIndex].IndexSearcher.Doc(hit.Doc)
                        let fields = GetDocument(indexWriter, search, document)
                        
                        let resultDoc : Document.T = 
                            { Id = document.Get(indexWriter.Settings.FieldsLookup.[Constants.IdField].SchemaName)
                              IndexName = indexWriter.Settings.IndexName
                              TimeStamp = 
                                  int64 
                                      (document.Get
                                           (indexWriter.Settings.FieldsLookup.[Constants.LastModifiedField].SchemaName))
                              Fields = fields
                              Score = 
                                  if search.ReturnScore then float (hit.Score)
                                  else 0.0
                              Highlights = null }
                        if highlighterOptions.IsSome then 
                            let (field, highlighter) = highlighterOptions.Value
                            let text = document.Get(field.SchemaName)
                            if text <> null then 
                                let tokenStream = 
                                    TokenSources.GetAnyTokenStream
                                        (indexSearchers.[hit.ShardIndex].IndexReader, hit.Doc, field.SchemaName, 
                                         indexWriter.Settings.SearchAnalyzer)
                                let frags = 
                                    highlighter.GetBestTextFragments
                                        (tokenStream, text, false, search.Highlights.FragmentsToReturn)
                                for frag in frags do
                                    if frag <> null && frag.GetScore() > float32 (0) then 
                                        resultDoc.Highlights.Add(frag.ToString())
                        yield resultDoc
            }
        for i in 0..indexSearchers.Length - 1 do
            (indexSearchers.[i] :> IDisposable).Dispose()
        Choice1Of2(results, recordsReturned, totalAvailable)
    
    let SearchDictionarySeq(indexWriter : IndexWriter.T, query : Query, search : SearchQuery.T) = 
        let (hits, _, recordsReturned, totalAvailable, indexSearchers) = Search(indexWriter, query, search)
        let skipped = ref 0
        
        let results = 
            seq { 
                for hit in hits do
                    if search.Skip > 0 && skipped.Value < search.Skip then skipped.Value <- skipped.Value + 1
                    else 
                        let document = indexSearchers.[hit.ShardIndex].IndexSearcher.Doc(hit.Doc)
                        let fields = GetDocument(indexWriter, search, document)
                        fields.Add
                            (Constants.IdField, 
                             document.Get(indexWriter.Settings.FieldsLookup.[Constants.IdField].SchemaName))
                        fields.Add
                            (Constants.LastModifiedField, 
                             document.Get(indexWriter.Settings.FieldsLookup.[Constants.LastModifiedField].SchemaName))
                        if search.ReturnScore then fields.Add("_score", hit.Score.ToString())
                        yield fields
            }
        for i in 0..indexSearchers.Length - 1 do
            (indexSearchers.[i] :> IDisposable).Dispose()
        ok (results, recordsReturned, totalAvailable)

[<AutoOpen>]
module QueryHelpers = 
    // Find terms associated with the search string
    let inline GetTerms(flexField : Field.T, value) = 
        match Field.getSearchAnalyzer (flexField) with
        | Some(a) -> parseTextUsingAnalyzer (a, flexField.SchemaName, value)
        | None -> new List<string>([ value ])
    
    let GetKeyValue(value : string) = 
        if (value.Contains(":")) then 
            Some(value.Substring(0, value.IndexOf(":")), value.Substring(value.IndexOf(":") + 1))
        else None
    
    let inline GetParametersAsDict(arr : string array, skip : int) = 
        let parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        arr 
        |> Array.iteri 
               (fun i x -> 
               if i >= skip && x.Contains(":") then 
                   parameters.Add(x.Substring(0, x.IndexOf(":")), x.Substring(x.IndexOf(":") + 1)))
        parameters
    
    let NumericTermQuery(flexIndexField : Field.T, value) = 
        match flexIndexField.FieldType with
        | FieldType.Date | FieldType.DateTime | FieldType.Long -> 
            match Int64.TryParse(value) with
            | (true, val1) -> 
                ok 
                    (NumericRangeQuery.NewLongRange
                         (flexIndexField.SchemaName, GetJavaLong(val1), GetJavaLong(val1), true, true) :> Query)
            | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
        | FieldType.Int -> 
            match Int32.TryParse(value) with
            | (true, val1) -> 
                Choice1Of2
                    (NumericRangeQuery.NewIntRange
                         (flexIndexField.SchemaName, GetJavaInt(val1), GetJavaInt(val1), true, true) :> Query)
            | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
        | FieldType.Double -> 
            match Double.TryParse(value) with
            | (true, val1) -> 
                Choice1Of2
                    (NumericRangeQuery.NewDoubleRange
                         (flexIndexField.SchemaName, GetJavaDouble(val1), GetJavaDouble(val1), true, true) :> Query)
            | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
        | _ -> fail (QueryOperatorFieldTypeNotSupported(flexIndexField.FieldName))

// ----------------------------------------------------------------------------
/// Term Query
// ----------------------------------------------------------------------------
[<Name("term_match")>]
[<Sealed>]
type FlexTermQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| "eq"; "=" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            match FieldType.isNumericField (flexIndexField.FieldType) with
            | true -> NumericTermQuery(flexIndexField, values.[0])
            | false -> 
                let terms = GetTerms(flexIndexField, values.[0])
                // If there are multiple terms returned by the parser then we will create a boolean query
                // with all the terms as sub clauses with And operator
                // This behaviour will result in matching of both the terms in the results which may not be
                // adjacent to each other. The adjacency case should be handled through phrase query
                match terms.Count with
                | 0 -> Choice1Of2(new MatchAllDocsQuery() :> Query)
                | 1 -> Choice1Of2(new TermQuery(new Term(flexIndexField.SchemaName, terms.[0])) :> Query)
                | _ -> 
                    // Generate boolean query
                    let boolClause = 
                        match parameters with
                        | Some(p) -> 
                            match p.TryGetValue("clausetype") with
                            | true, b -> 
                                match b with
                                | InvariantEqual "or" -> BooleanClause.Occur.SHOULD
                                | _ -> BooleanClause.Occur.MUST
                            | _ -> BooleanClause.Occur.MUST
                        | _ -> BooleanClause.Occur.MUST
                    
                    let boolQuery = new BooleanQuery()
                    for term in terms do
                        boolQuery.Add
                            (new BooleanClause(new TermQuery(new Term(flexIndexField.SchemaName, term)), boolClause))
                    Choice1Of2(boolQuery :> Query)

// ----------------------------------------------------------------------------
/// Fuzzy Query
// ---------------------------------------------------------------------------- 
[<Name("fuzzy_match")>]
[<Sealed>]
type FlexFuzzyQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| "fuzzy"; "~=" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            let terms = GetTerms(flexIndexField, values.[0])
            let slop = GetIntValueFromMap parameters "slop" 1
            let prefixLength = GetIntValueFromMap parameters "prefixlength" 0
            match terms.Count with
            | 0 -> Choice1Of2(new MatchAllDocsQuery() :> Query)
            | 1 -> 
                Choice1Of2(new FuzzyQuery(new Term(flexIndexField.SchemaName, terms.[0]), slop, prefixLength) :> Query)
            | _ -> 
                // Generate boolean query
                let boolQuery = new BooleanQuery()
                for term in terms do
                    boolQuery.Add
                        (new BooleanClause(new FuzzyQuery(new Term(flexIndexField.SchemaName, term), slop, prefixLength), 
                                           BooleanClause.Occur.MUST))
                Choice1Of2(boolQuery :> Query)

// ----------------------------------------------------------------------------
/// Match all Query
// ---------------------------------------------------------------------------- 
[<Name("match_all")>]
[<Sealed>]
type FlexMatchAllQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| "matchall" |]
        member this.GetQuery(flexIndexField, values, parameters) = Choice1Of2(new MatchAllDocsQuery() :> Query)

// ----------------------------------------------------------------------------
/// Phrase Query
// ---------------------------------------------------------------------------- 
[<Name("phrase_match")>]
[<Sealed>]
type FlexPhraseQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| "match" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            let terms = GetTerms(flexIndexField, values.[0])
            let query = new PhraseQuery()
            for term in terms do
                query.add (new Term(flexIndexField.SchemaName, term))
            let slop = GetIntValueFromMap parameters "slop" 0
            query.setSlop (slop)
            Choice1Of2(query :> Query)

// ----------------------------------------------------------------------------
/// Wildcard Query
// ---------------------------------------------------------------------------- 
[<Name("like")>]
[<Sealed>]
type FlexWildcardQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| "like"; "%=" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            // Like query does not go through analysis phase as the analyzer would remove the
            // special character
            match values.Count() with
            | 0 -> Choice1Of2(new MatchAllDocsQuery() :> Query)
            | 1 -> 
                Choice1Of2
                    (new WildcardQuery(new Term(flexIndexField.SchemaName, values.[0].ToLowerInvariant())) :> Query)
            | _ -> 
                // Generate boolean query
                let boolQuery = new BooleanQuery()
                for term in values do
                    boolQuery.add 
                        (new WildcardQuery(new Term(flexIndexField.SchemaName, term.ToLowerInvariant())), 
                         BooleanClause.Occur.MUST)
                Choice1Of2(boolQuery :> Query)

// ----------------------------------------------------------------------------
/// Regex Query
// ---------------------------------------------------------------------------- 
[<Name("regex")>]
[<Sealed>]
type RegexQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| "regex" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            // Regex query does not go through analysis phase as the analyzer would remove the
            // special character
            match values.Count() with
            | 0 -> Choice1Of2(new MatchAllDocsQuery() :> Query)
            | 1 -> 
                Choice1Of2(new RegexpQuery(new Term(flexIndexField.SchemaName, values.[0].ToLowerInvariant())) :> Query)
            | _ -> 
                // Generate boolean query
                let boolQuery = new BooleanQuery()
                for term in values do
                    boolQuery.add 
                        (new RegexpQuery(new Term(flexIndexField.SchemaName, term.ToLowerInvariant())), 
                         BooleanClause.Occur.MUST)
                Choice1Of2(boolQuery :> Query)

// ----------------------------------------------------------------------------
// Range Queries
// ---------------------------------------------------------------------------- 
[<Name("greater")>]
[<Sealed>]
type FlexGreaterQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| ">" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            // Greater query does not go through analysis phase as the analyzer would remove the
            // special character
            let includeLower = false
            let includeUpper = true
            match FieldType.isNumericField (flexIndexField.FieldType) with
            | true -> 
                match flexIndexField.FieldType with
                | FieldType.Date | FieldType.DateTime | FieldType.Long -> 
                    match Int64.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.NewLongRange
                                 (flexIndexField.SchemaName, GetJavaLong(val1), JavaLongMax, includeLower, includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | FieldType.Int -> 
                    match Int32.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.NewIntRange
                                 (flexIndexField.SchemaName, GetJavaInt(val1), JavaIntMax, includeLower, includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | FieldType.Double -> 
                    match Double.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.NewDoubleRange
                                 (flexIndexField.SchemaName, GetJavaDouble(val1), JavaDoubleMax, includeLower, 
                                  includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
            | false -> fail (DataCannotBeParsed(flexIndexField.FieldName))

[<Name("greater_than_equal")>]
[<Sealed>]
type FlexGreaterThanEqualQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| ">=" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            // Greater query does not go through analysis phase as the analyzer would remove the
            // special character
            let includeLower = true
            let includeUpper = true
            match FieldType.isNumericField (flexIndexField.FieldType) with
            | true -> 
                match flexIndexField.FieldType with
                | FieldType.Date | FieldType.DateTime | FieldType.Long -> 
                    match Int64.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newLongRange 
                                 (flexIndexField.SchemaName, GetJavaLong(val1), JavaLongMax, includeLower, includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | FieldType.Int -> 
                    match Int32.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newIntRange 
                                 (flexIndexField.SchemaName, GetJavaInt(val1), JavaIntMax, includeLower, includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | FieldType.Double -> 
                    match Double.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newDoubleRange 
                                 (flexIndexField.SchemaName, GetJavaDouble(val1), JavaDoubleMax, includeLower, 
                                  includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
            | false -> fail (DataCannotBeParsed(flexIndexField.FieldName))

[<Name("less_than")>]
[<Sealed>]
type FlexLessThanQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| "<" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            // Greater query does not go through analysis phase as the analyzer would remove the
            // special character
            let includeLower = true
            let includeUpper = false
            match FieldType.isNumericField (flexIndexField.FieldType) with
            | true -> 
                match flexIndexField.FieldType with
                | FieldType.Date | FieldType.DateTime | FieldType.Long -> 
                    match Int64.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newLongRange 
                                 (flexIndexField.SchemaName, JavaLongMin, GetJavaLong(val1), includeLower, includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | FieldType.Int -> 
                    match Int32.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newIntRange 
                                 (flexIndexField.SchemaName, JavaIntMin, GetJavaInt(val1), includeLower, includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | FieldType.Double -> 
                    match Double.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newDoubleRange 
                                 (flexIndexField.SchemaName, JavaDoubleMin, GetJavaDouble(val1), includeLower, 
                                  includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
            | false -> fail (DataCannotBeParsed(flexIndexField.FieldName))

[<Name("less_than_equal")>]
[<Sealed>]
type FlexLessThanEqualQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| "<=" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            // Greater query does not go through analysis phase as the analyzer would remove the
            // special character
            let includeLower = true
            let includeUpper = true
            match FieldType.isNumericField (flexIndexField.FieldType) with
            | true -> 
                match flexIndexField.FieldType with
                | FieldType.Date | FieldType.DateTime | FieldType.Long -> 
                    match Int64.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newLongRange 
                                 (flexIndexField.SchemaName, JavaLongMin, GetJavaLong(val1), includeLower, includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | FieldType.Int -> 
                    match Int32.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newIntRange 
                                 (flexIndexField.SchemaName, JavaIntMin, GetJavaInt(val1), includeLower, includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | FieldType.Double -> 
                    match Double.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newDoubleRange 
                                 (flexIndexField.SchemaName, JavaDoubleMin, GetJavaDouble(val1), includeLower, 
                                  includeUpper) :> Query)
                    | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
                | _ -> fail (DataCannotBeParsed(flexIndexField.FieldName))
            | false -> fail (DataCannotBeParsed(flexIndexField.FieldName))
