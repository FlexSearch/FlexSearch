// ----------------------------------------------------------------------------
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
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.IO
open System.Linq
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open java.io
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.util
open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

[<AutoOpen>]
[<RequireQualifiedAccess>]
/// Exposes high level operations that can performed across the system.
/// Most of the services basically act as a wrapper around the functions 
/// here. Care should be taken to not introduce any mutable state in the
/// module but to only pass mutable state as an instance of NodeState
module SearchService = 
    // ----------------------------------------------------------------------------
    // Search service class which will be dynamically injected using IOC. This will
    // provide the interface for all kind of search functionality in flex.
    // ----------------------------------------------------------------------------    
    type SearchService(nodeState : INodeState, queryFactory : IFlexFactory<IFlexQuery>, queryParsersPool : ObjectPool<FlexParser>) = 
        let queryTypes = queryFactory.GetAllModules()
        
        let search (flexIndex : FlexIndex, search : SearchQuery) = 
            maybe { 
                if String.IsNullOrWhiteSpace(search.SearchProfile) <> true then 
                    // Search profile based
                    match flexIndex.IndexSetting.SearchProfiles.TryGetValue(search.SearchProfile) with
                    | true, p -> 
                        let (p', sq) = p
                        search.MissingValueConfiguration <- sq.MissingValueConfiguration
                        let! values = Parsers.ParseQueryString(search.QueryString)
                        let! query = SearchDsl.GenerateQuery flexIndex p' search (Some(values)) queryTypes
                        return! SearchDsl.SearchQuery(flexIndex, query, search)
                    | _ -> return! Choice2Of2(MessageConstants.SEARCH_PROFILE_NOT_FOUND)
                else 
                    use parser = queryParsersPool.Acquire()
                    let! predicate = parser.Parse(search.QueryString)
                    parser.Release()
                    match predicate with
                    | NotPredicate(_) -> return! Choice2Of2(MessageConstants.NEGATIVE_QUERY_NOT_SUPPORTED)
                    | _ -> let! query = GenerateQuery flexIndex predicate search None queryTypes
                           return! SearchDsl.SearchQuery(flexIndex, query, search)
            }
        
        interface ISearchService with
            member this.Search(query) = maybe { let! flexIndex = nodeState.IndicesState.GetRegisteration
                                                                     (query.IndexName)
                                                return! search (flexIndex, query) }
            member this.Search(flexIndex, query) = search (flexIndex, query)
