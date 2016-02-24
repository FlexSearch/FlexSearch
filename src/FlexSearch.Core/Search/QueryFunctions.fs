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
        match tokens.Count() with
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
            p.Add(getTerm fieldSchema.SchemaName tokens.Segments.[0])
            ok <| (p :> Query)
        | _ -> 
            let q = getBooleanQuery()
            for (startPos, len) in tokens.Positions do
                let p = getPhraseQuery slop
                for i = startPos to startPos + len do
                    p.Add(getTerm fieldSchema.SchemaName tokens.Segments.[i])
                addBooleanClause p BooleanClauseOccur.SHOULD q |> ignore
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

/// Simple phrase match query which allows positional match of tokens
[<Name("uptonwordsapart"); Sealed>]
type UpToNWordsApartQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = true
        member __.GetNumericQuery(fieldSchema, _, _) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "uptoNWordsApart")
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            let slop = parameters |> intFromParameters "slop" 1
            phraseMatch slop fieldSchema tokens

/// Specialized case of UpToNWordsApart query where slop is hard coded to 1
[<Name("exact"); Sealed>]
type ExactQuery() = 
    interface IQueryFunction with
        member __.UseAnalyzer = true
        member __.GetNumericQuery(fieldSchema, _, _) = 
            fail <| QueryOperatorFieldTypeNotSupported(fieldSchema.FieldName, "exact")
        member __.GetQuery(fieldSchema, tokens, parameters) = 
            phraseMatch 1 fieldSchema tokens
            
///// Wildcard Query
//[<Name("like"); Sealed>]
//type FlexWildcardQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, _) =
//            ignoreOrExecuteFunction arguments
//            <| fun _ -> 
//                arguments |> checkAtLeastNPopulatedArguments 1 __
//                >>= fun _ -> 
//                    // Like query does not go through analysis phase as the analyzer would remove the
//                    // special character
//                    zeroOneOrManyQuery 
//                    <| (arguments |> getPopulatedArguments |> Seq.map (fun x -> x.ToLowerInvariant())) 
//                    <| getWildCardQuery fieldSchema.SchemaName 
//                    <| BooleanClauseOccur.MUST
//
///// Regex Query
//[<Name("regex"); Sealed>]
//type RegexQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, _) = 
//            ignoreOrExecuteFunction arguments
//            <| fun _ -> 
//                // Regex query does not go through analysis phase as the analyzer would remove the
//                // special character
//                zeroOneOrManyQuery 
//                <| (arguments |> getPopulatedArguments |> Seq.map (fun x -> x.ToLowerInvariant())) 
//                <| getRegexpQuery fieldSchema.SchemaName 
//                <| BooleanClauseOccur.MUST
//
//// ----------------------------------------------------------------------------
//// Range Queries
//// Note: These queries don't go through analysis phase as the analyzer would 
//// remove the special character
//// ---------------------------------------------------------------------------- 
//[<Name("gt"); Sealed>]
//type FlexGreaterQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, _) =
//            ignoreOrExecuteFunction arguments
//            <| fun _ -> 
//                arguments |> checkItHasNPopulatedArguments 1 __
//                >>= fun _ ->
//                    fieldSchema.FieldType.GetRangeQuery.Value fieldSchema.SchemaName 
//                                                              (arguments.[0].Value, Constants.Infinite) 
//                                                              (false, true)
//
//[<Name("ge"); Sealed>]
//type FlexGreaterThanEqualQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, _) =
//            ignoreOrExecuteFunction arguments
//            <| fun _ -> 
//                arguments |> checkItHasNPopulatedArguments 1 __
//                >>= fun _ ->
//                    fieldSchema.FieldType.GetRangeQuery.Value fieldSchema.SchemaName 
//                                                              (arguments.[0].Value, Constants.Infinite) 
//                                                              (true, true)
//
//[<Name("lt"); Sealed>]
//type FlexLessThanQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, _) = 
//            ignoreOrExecuteFunction arguments
//            <| fun _ -> 
//                arguments |> checkItHasNPopulatedArguments 1 __
//                >>= fun _ ->
//                    fieldSchema.FieldType.GetRangeQuery.Value fieldSchema.SchemaName 
//                                                              (Constants.Infinite, arguments.[0].Value) 
//                                                              (true, false)
//
//[<Name("le"); Sealed>]
//type FlexLessThanEqualQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, _) = 
//            ignoreOrExecuteFunction arguments
//            <| fun _ -> 
//                arguments |> checkItHasNPopulatedArguments 1 __
//                >>= fun _ ->
//                    fieldSchema.FieldType.GetRangeQuery.Value fieldSchema.SchemaName 
//                                                              (Constants.Infinite, arguments.[0].Value) 
//                                                              (true, true)
