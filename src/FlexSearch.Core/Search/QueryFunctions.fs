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

open System
open SearchQueryHelpers
open FlexLucene.Search

// ----------------------------------------------------------------------------
// Query Functions
// These functions modify a Lucene query by setting various parameters and 
// returning the query back
// ---------------------------------------------------------------------------- 

[<Name("boost"); Sealed>]
type BoostFunc() =
    interface IQueryFunction with
        member __.GetQuery(query : Query, argument : ComputedValue) = 
            argument 
            |> byDefault "1"
            |> getNumeric __ 0
            >>= fun boost -> query.SetBoost(float32 boost); ok query

[<Name("constantscore"); Sealed>]
type ConstantScoreFunc() =
    interface IQueryFunction with
        member __.GetQuery(query : Query, argument : ComputedValue) = 
            argument 
            |> byDefault "1"
            |> getNumeric __ 0
            >>= fun boost -> let q = new ConstantScoreQuery(query) :> Query
                             q.SetBoost(float32 boost)
                             ok q

[<Name("filter"); Sealed>]
type FilterFunc() =
    interface IQueryFunction with
        member __.GetQuery(query : Query, argument : ComputedValue) = 
            argument 
            |> byDefault "1"
            |> getNumeric __ 0
            >>= fun boost -> new BooleanQuery()
                             |> addFilterClause query
                             :> Query
                             |> ok
                

