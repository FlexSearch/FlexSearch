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

// ----------------------------------------------------------------------------
// Field Functions
// These functions generate a Lucene Query based on input
// ---------------------------------------------------------------------------- 

[<AutoOpen>]
module Common =
    let anyAllOfBase fieldSchema arguments (instance : 'T) isAllOf = ()
//        match FieldSchema.isNumericField fieldSchema with
//        | true -> 
//            arguments |> checkItHasNPopulatedArguments 1 instance
//            >>= fun _ -> fieldSchema.FieldType.GetRangeQuery.Value fieldSchema.SchemaName 
//                                                                   (arguments.[0].Value, arguments.[0].Value) 
//                                                                   (true, true)
//        | false -> 
//            // If there are multiple terms returned by the parser then we will create a boolean query
//            // with all the terms as sub clauses with And operator
//            // This behaviour will result in matching of both the terms in the results which may not be
//            // adjacent to each other. The adjacency case should be handled through phrase query
//            arguments |> checkAtLeastNPopulatedArguments 1 instance
//            >>= fun _ -> 
//                zeroOneOrManyQuery 
//                <| FieldSchema.getTerms (arguments |> getPopulatedArguments, new List<string>()) fieldSchema 
//                <| getTermQuery fieldSchema.SchemaName 
//                <| if isAllOf then BooleanClauseOccur.MUST else BooleanClauseOccur.SHOULD
//
//    let phraseMatch fieldSchema arguments (instance : 'T) funcName slop =
//        arguments |> checkAtLeastNPopulatedArguments 1 instance
//        >>= fun _ -> 
//            let terms = FieldSchema.getTerms (arguments |> getPopulatedArguments, new List<string>()) fieldSchema 
//            let query = new PhraseQuery()
//            for term in terms do
//                query.Add(new Term(fieldSchema.SchemaName, term))
//            query.SetSlop(slop)
//            ok <| (query :> Query)



///// Term Query
//[<Name("allof"); Sealed>]
//type AllOfQuery() = 
//    interface IQueryFunction with
//        member __.GetQuery(fieldSchema, arguments, _) = 
//            ignoreOrExecuteFunction arguments
//            <| fun _ -> anyAllOfBase fieldSchema arguments __ true
//
//[<Name("anyof"); Sealed>]
//type AnyOfQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, _) = 
//            ignoreOrExecuteFunction arguments
//            <| fun _ -> anyAllOfBase fieldSchema arguments __ false
//            
//
///// Fuzzy Query
//[<Name("fuzzy"); Sealed>]
//type FlexFuzzyQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, funcName) =
//            ignoreOrExecuteFunction arguments
//            <| fun _ ->
//                let slop = extractDigits funcName |> byDefault 1
//                // TODO
//                //let prefixLength = parameters |> intFromOptDict "prefixlength" 0
//                arguments |> checkAtLeastNPopulatedArguments 1 __
//                >>= fun _ -> 
//                    zeroOneOrManyQuery 
//                    <| FieldSchema.getTerms (arguments |> getPopulatedArguments, new List<string>()) fieldSchema 
//                    <| getFuzzyQuery fieldSchema.SchemaName slop 0 <| BooleanClauseOccur.MUST
//
///// Match all Query
//[<Name("matchall"); Sealed>]
//type FlexMatchAllQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(_,_,_) = ok <| getMatchAllDocsQuery()
//
//[<Name("uptowordsapart"); Sealed>]
//type UpToNWordsApartQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, funcName) =
//            ignoreOrExecuteFunction arguments
//            <| fun _ ->  
//                let slop = extractDigits funcName |> byDefault 0
//                phraseMatch fieldSchema arguments __ funcName slop
//
//[<Name("exact"); Sealed>]
//type ExactMatchQuery() = 
//    interface IFieldFunction with
//        member __.GetQuery(fieldSchema, arguments, funcName) = 
//            ignoreOrExecuteFunction arguments
//            <| fun _ -> phraseMatch fieldSchema arguments __ funcName 0
//
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
