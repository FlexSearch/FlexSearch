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
open Validator

[<AutoOpen>]
module SearchQueryExtensions = 
    type SearchQuery with
        
        /// <summary>
        /// Validate a search query. This will be used as apart of SettingBuilder creation.
        /// Most of the related validation has to performed at search time.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="parser"></param>
        member this.Validate(fields : Dictionary<string, FlexField>, queryTypes : Dictionary<string, IFlexQuery>, 
                             parser : IFlexParser) = 
            maybe { 
                assert (queryTypes.Count > 0)
                do! ("QueryString", this.QueryString) |> NotNullAndEmpty
                assert (String.IsNullOrWhiteSpace(this.QueryString) <> true)
                let! queryPredicate = parser.Parse(this.QueryString)
                // Check if query fields are valid
                //let! query = SearchDsl.GenerateQuery(fields, queryPredicate, this, None, queryTypes)
                return! Choice1Of2()
            }
        
        member this.Build(fields : Dictionary<string, FlexField>, queryTypes : Dictionary<string, IFlexQuery>, 
                          parser : IFlexParser) = 
            maybe { 
                do! this.Validate(fields, queryTypes, parser)
                let! predicate = parser.Parse(this.QueryString)
                return predicate
            }
        
        static member Build(profiles : Dictionary<string, SearchQuery>, fields : Dictionary<string, FlexField>, 
                            queryTypes : Dictionary<string, IFlexQuery>, parser : FlexParser) = 
            maybe { 
                let result = new Dictionary<string, Predicate * SearchQuery>(StringComparer.OrdinalIgnoreCase)
                for profile in profiles do
                    let! profileObject = profile.Value.Build(fields, queryTypes, parser)
                    result.Add(profile.Key, (profileObject, profile.Value))
                return result
            }
        
        static member QueryTypes(factoryCollection : IFactoryCollection) = 
            let queryTypes = new Dictionary<string, IFlexQuery>(StringComparer.OrdinalIgnoreCase)
            for query in factoryCollection.SearchQueryFactory.GetAllModules() do
                for queryName in query.Value.QueryName() do
                    queryTypes.Add(queryName, query.Value)
            queryTypes
