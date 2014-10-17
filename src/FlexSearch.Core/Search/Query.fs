// ----------------------------------------------------------------------------
// Flexsearch predefined queries (Queries.fs)
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
/// Term Query
// ----------------------------------------------------------------------------
[<Name("term_match")>]
[<Sealed>]
type FlexTermQuery() = 
    interface IFlexQuery with
        member this.QueryName() = [| "eq"; "=" |]
        member this.GetQuery(flexIndexField, values, parameters) = 
            match IsNumericField(flexIndexField) with
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
                        boolQuery.add 
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
                    boolQuery.add 
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
            match IsNumericField(flexIndexField) with
            | true -> 
                match flexIndexField.FieldType with
                | FlexDate | FlexDateTime -> 
                    match Int64.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newLongRange 
                                 (flexIndexField.SchemaName, GetJavaLong(val1), JavaLongMax, includeLower, includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | FlexInt -> 
                    match Int32.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newIntRange 
                                 (flexIndexField.SchemaName, GetJavaInt(val1), JavaIntMax, includeLower, includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | FlexDouble -> 
                    match Double.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newDoubleRange 
                                 (flexIndexField.SchemaName, GetJavaDouble(val1), JavaDoubleMax, includeLower, 
                                  includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | _ -> 
                    Choice2Of2(Errors.QUERY_OPERATOR_FIELD_TYPE_NOT_SUPPORTED
                               |> GenerateOperationMessage
                               |> Append("Field Name", flexIndexField.FieldName))
            | false -> 
                Choice2Of2(Errors.QUERY_OPERATOR_FIELD_TYPE_NOT_SUPPORTED
                           |> GenerateOperationMessage
                           |> Append("Field Name", flexIndexField.FieldName))

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
            match IsNumericField(flexIndexField) with
            | true -> 
                match flexIndexField.FieldType with
                | FlexDate | FlexDateTime -> 
                    match Int64.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newLongRange 
                                 (flexIndexField.SchemaName, GetJavaLong(val1), JavaLongMax, includeLower, includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | FlexInt -> 
                    match Int32.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newIntRange 
                                 (flexIndexField.SchemaName, GetJavaInt(val1), JavaIntMax, includeLower, includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | FlexDouble -> 
                    match Double.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newDoubleRange 
                                 (flexIndexField.SchemaName, GetJavaDouble(val1), JavaDoubleMax, includeLower, 
                                  includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | _ -> 
                    Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                               |> GenerateOperationMessage
                               |> Append("Field Name", flexIndexField.FieldName))
            | false -> 
                Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                           |> GenerateOperationMessage
                           |> Append("Field Name", flexIndexField.FieldName))

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
            match IsNumericField(flexIndexField) with
            | true -> 
                match flexIndexField.FieldType with
                | FlexDate | FlexDateTime -> 
                    match Int64.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newLongRange 
                                 (flexIndexField.SchemaName, JavaLongMin, GetJavaLong(val1), includeLower, includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | FlexInt -> 
                    match Int32.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newIntRange 
                                 (flexIndexField.SchemaName, JavaIntMin, GetJavaInt(val1), includeLower, includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | FlexDouble -> 
                    match Double.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newDoubleRange 
                                 (flexIndexField.SchemaName, JavaDoubleMin, GetJavaDouble(val1), includeLower, 
                                  includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | _ -> 
                    Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                               |> GenerateOperationMessage
                               |> Append("Field Name", flexIndexField.FieldName))
            | false -> 
                Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                           |> GenerateOperationMessage
                           |> Append("Field Name", flexIndexField.FieldName))

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
            match IsNumericField(flexIndexField) with
            | true -> 
                match flexIndexField.FieldType with
                | FlexDate | FlexDateTime -> 
                    match Int64.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newLongRange 
                                 (flexIndexField.SchemaName, JavaLongMin, GetJavaLong(val1), includeLower, includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | FlexInt -> 
                    match Int32.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newIntRange 
                                 (flexIndexField.SchemaName, JavaIntMin, GetJavaInt(val1), includeLower, includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | FlexDouble -> 
                    match Double.TryParse(values.[0]) with
                    | true, val1 -> 
                        Choice1Of2
                            (NumericRangeQuery.newDoubleRange 
                                 (flexIndexField.SchemaName, JavaDoubleMin, GetJavaDouble(val1), includeLower, 
                                  includeUpper) :> Query)
                    | _ -> 
                        Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                                   |> GenerateOperationMessage
                                   |> Append("Field Name", flexIndexField.FieldName))
                | _ -> 
                    Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                               |> GenerateOperationMessage
                               |> Append("Field Name", flexIndexField.FieldName))
            | false -> 
                Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                           |> GenerateOperationMessage
                           |> Append("Field Name", flexIndexField.FieldName))
