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

open FlexSearch.Core
open FlexLucene.Index
open FlexLucene.Search
open System.Collections.Generic
open SearchQueryHelpers
open FlexLucene.Search
open System.Linq
open System

// ----------------------------------------------------------------------------
// Field Functions
// These functions generate a Lucene Query based on input
// ---------------------------------------------------------------------------- 
[<AutoOpen>]
module Common = 
    /// Get integer value for a key from the clause
    let inline intFromParameters key defaultValue (parameters : ClauseProperties) = 
        let f = 
            parameters.Switches.FirstOrDefault
                (fun x -> String.Equals(x.Name, key, StringComparison.InvariantCultureIgnoreCase))
        if isNotNull f then 
            match f.Value with
            | Some(v) -> pInt defaultValue v
            | None -> defaultValue
        else defaultValue
    
    /// Checks if a given switch exists    
    let inline switchExists key (parameters : ClauseProperties) = 
        let f = 
            parameters.Switches.FirstOrDefault
                (fun x -> String.Equals(x.Name, key, StringComparison.InvariantCultureIgnoreCase))
        if isNotNull f then Some()
        else None
    
    // ----------------------------------------------------------------------------
    // Query generators
    // ----------------------------------------------------------------------------
    let getBoolQueryFromTerms boolClauseType innerQueryProvider terms = 
        let boolQuery = getBooleanQuery()
        terms |> Seq.iter (fun term -> 
                     let innerQuery = innerQueryProvider term
                     boolQuery
                     |> addBooleanClause innerQuery boolClauseType
                     |> ignore)
        boolQuery :> Query
    
    /// Generates simple or boolean query depending upon the number of tokens.
    /// NOTE: This should only be used when generated query is term based with
    /// no positional relevance
    let zeroOneOrManyQuery (tokens : Tokens) innerQueryProvider boolClause = 
        match tokens.Segments.Count with
        | 0 -> getMatchAllDocsQuery()
        | 1 -> innerQueryProvider (tokens.Segments.[0])
        | _ -> getBoolQueryFromTerms boolClause innerQueryProvider tokens.Segments
        |> ok
    
    let phraseMatch (slop) (fieldSchema : FieldSchema) (tokens : Tokens) = 
        assert (tokens.Count() > 0)
        match tokens.Count() with
        | 0 -> failwithf "Query should never be called with 0 tokens"
        | 1 -> 
            let p = getPhraseQuery slop
            for t in tokens.Segments do
                p.Add(getTerm fieldSchema.SchemaName t)
            ok <| (p :> Query)
        | _ -> 
            let q = getBooleanQuery()
            for (startPos, len) in tokens.Positions do
                let p = getPhraseQuery slop
                for i = startPos to startPos + len - 1 do
                    p.Add(getTerm fieldSchema.SchemaName tokens.Segments.[i])
                addBooleanClause p BooleanClauseOccur.SHOULD q |> ignore
            ok <| (q :> Query)
    
    let multiPhraseMatch (slop) (fieldSchema : FieldSchema) (tokens : Tokens) = 
        assert (tokens.Count() > 0)
        let q = new MultiPhraseQuery()
        q.SetSlop slop
        for (startPos, len) in tokens.Positions do
            let terms = new List<Term>()
            for i = startPos to startPos + len - 1 do
                terms.Add(getTerm fieldSchema.SchemaName tokens.Segments.[i])
            q.Add(terms.ToArray())
        ok <| (q :> Query)

/// AllOf Query is useful for matching all terms in the input
/// in any order
[<Name("allof"); Sealed>]
type AllOfQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = true
        member __.GetNumericQuery(fieldSchema, tokens, parameters) = 
            fieldSchema.FieldType.GetNumericBooleanQuery fieldSchema.SchemaName tokens.Segments BooleanClauseOccur.MUST
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            zeroOneOrManyQuery tokens (getTermQuery fieldSchema.SchemaName) BooleanClauseOccur.MUST

/// AnyOf Query is useful for matching any number terms in the input
/// in any order
[<Name("anyof"); Sealed>]
type AnyOfQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = true
        member __.GetNumericQuery(fieldSchema, tokens, parameters) = 
            fieldSchema.FieldType.GetNumericBooleanQuery fieldSchema.SchemaName tokens.Segments 
                BooleanClauseOccur.SHOULD
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            zeroOneOrManyQuery tokens (getTermQuery fieldSchema.SchemaName) BooleanClauseOccur.SHOULD

/// Fuzzy Query is useful for fuzzy matching any number of terms in the input
/// in any order
[<Name("fuzzy"); Sealed>]
type FuzzyQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = true
        member __.GetNumericQuery(fieldSchema, _, _) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "fuzzy")
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            let slop = parameters |> intFromParameters "slop" 1
            let prefixLength = parameters |> intFromParameters "prefixLength" 0
            zeroOneOrManyQuery tokens (getFuzzyQuery fieldSchema.SchemaName slop prefixLength) BooleanClauseOccur.SHOULD

/// Match all Query
[<Name("matchall"); Sealed>]
type MatchAllQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = false
        member __.GetNumericQuery(_, _, _) = ok <| getMatchAllDocsQuery()
        member __.GetQuery(_, _, _) = ok <| getMatchAllDocsQuery()

/// Match none Query
[<Name("matchnone"); Sealed>]
type MatchNoneQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = false
        member __.GetNumericQuery(_, _, _) = ok <| getMatchNoDocsQuery()
        member __.GetQuery(_, _, _) = ok <| getMatchNoDocsQuery()

/// Phrase match query which allows positional match of tokens.
/// It also supports multi phrase query which allows multiple
/// terms to be matched at the same position.
[<Name("phrasematch"); Sealed>]
type PhraseMatchQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = true
        member __.GetNumericQuery(fieldSchema, _, _) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "phraseMatch")
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            let slop = parameters |> intFromParameters "slop" 1
            let multiPhrase = parameters |> switchExists "multiphrase"
            match multiPhrase with
            | Some _ -> multiPhraseMatch slop fieldSchema tokens
            | None -> phraseMatch slop fieldSchema tokens

/// Wild card Query
[<Name("like"); Sealed>]
type WildcardQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = false
        member __.GetNumericQuery(fieldSchema, _, _) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "like")
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            zeroOneOrManyQuery tokens (getWildCardQuery fieldSchema.SchemaName) BooleanClauseOccur.SHOULD

/// Regex Query
[<Name("regex"); Sealed>]
type RegexQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = false
        member __.GetNumericQuery(fieldSchema, _, _) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "regex")
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            zeroOneOrManyQuery tokens (getRegexpQuery fieldSchema.SchemaName) BooleanClauseOccur.SHOULD

//// ----------------------------------------------------------------------------
//// Range Queries
//// ---------------------------------------------------------------------------- 
[<Name("gt"); Sealed>]
type GreaterThanQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = false
        member __.GetNumericQuery(fieldSchema, tokens, _) = 
            fieldSchema.FieldType.GetRangeQuery fieldSchema.SchemaName tokens.Segments.[0] Constants.Infinite false true
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "gt")

[<Name("ge"); Sealed>]
type GreaterThanEqualQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = false
        member __.GetNumericQuery(fieldSchema, tokens, _) = 
            fieldSchema.FieldType.GetRangeQuery fieldSchema.SchemaName tokens.Segments.[0] Constants.Infinite true true
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "ge")

[<Name("lt"); Sealed>]
type LessThanQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = false
        member __.GetNumericQuery(fieldSchema, tokens, _) = 
            fieldSchema.FieldType.GetRangeQuery fieldSchema.SchemaName Constants.Infinite tokens.Segments.[0] true false
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "lt")

[<Name("le"); Sealed>]
type LessThanEqualQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = false
        member __.GetNumericQuery(fieldSchema, tokens, _) = 
            fieldSchema.FieldType.GetRangeQuery fieldSchema.SchemaName Constants.Infinite tokens.Segments.[0] true true
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "le")
