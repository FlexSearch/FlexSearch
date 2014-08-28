// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
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
open System.Collections.Generic
open System
open FlexSearch.Common

[<AutoOpen>]
module SearchQueryExtensions = 
    type SearchQuery with
        
        member this.Build(queryTypes : Dictionary<string, IFlexQuery>, parser : IFlexParser) = 
            maybe { 
                assert (queryTypes.Count > 0)
                assert (String.IsNullOrWhiteSpace(this.QueryString) <> true)
                let! predicate = parser.Parse(this.QueryString)
                return predicate
            }
        
        static member Build(profiles : Dictionary<string, SearchQuery>, fields : Dictionary<string, FlexField>, 
                            queryTypes : Dictionary<string, IFlexQuery>, parser : FlexParser) = 
            maybe { 
                let result = new Dictionary<string, Predicate * SearchQuery>(StringComparer.OrdinalIgnoreCase)
                for profile in profiles do
                    let! profileObject = profile.Value.Build(queryTypes, parser)
                    result.Add(profile.Key, (profileObject, profile.Value))
                return result
            }
        
        static member QueryTypes(factoryCollection : IFactoryCollection) = 
            let queryTypes = new Dictionary<string, IFlexQuery>(StringComparer.OrdinalIgnoreCase)
            for query in factoryCollection.SearchQueryFactory.GetAllModules() do
                for queryName in query.Value.QueryName() do
                    queryTypes.Add(queryName, query.Value)
            queryTypes
