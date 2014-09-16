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
namespace FlexSearch.Core.HttpHandlers

open FlexSearch.Api
open FlexSearch.Common
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Utility
open Microsoft.Owin
open Newtonsoft.Json
open Owin
open System
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.Composition
open System.IO
open System.Linq
open System.Net
open System.Net.Http

[<Name("GET|POST-/indices/:id/search")>]
[<Sealed>]
type GetSearchHandler(searchService : ISearchService) = 
    inherit HttpHandlerBase<SearchQuery, obj>()
    override this.Process(id, subId, body, context) = 
        let processRequest (indexName, owin : IOwinContext) = 
            maybe { 
                let query = 
                    match body with
                    | Some(q) -> q
                    | None -> 
                        // It is possible that the query is supplied through query-string
                        new SearchQuery()
                query.QueryString <- GetValueFromQueryString "q" query.QueryString owin
                query.Columns <- match owin.Request.Query.Get("c") with
                                 | null -> query.Columns
                                 | v -> v.Split([| ',' |], System.StringSplitOptions.RemoveEmptyEntries).ToList()
                query.Count <- GetIntValueFromQueryString "count" query.Count owin
                query.Skip <- GetIntValueFromQueryString "skip" query.Skip owin
                query.OrderBy <- GetValueFromQueryString "orderby" query.OrderBy owin
                query.ReturnFlatResult <- GetBoolValueFromQueryString "returnflatresult" query.ReturnFlatResult owin
                query.IndexName <- indexName
                match searchService.Search(query) with
                | Choice1Of2(v') -> 
                    if query.ReturnFlatResult then 
                        owin.Response.Headers.Add("RecordsReturned", [| v'.RecordsReturned.ToString() |])
                        owin.Response.Headers.Add("TotalAvailable", [| v'.TotalAvailable.ToString() |])
                        let result = v'.Documents |> Seq.map (fun x -> x.Fields)
                        return! Choice1Of2(result :> obj)
                    else return! Choice1Of2(v' :> obj)
                | Choice2Of2(e) -> return! Choice2Of2(e)
            }
        (processRequest (id.Value, context), Ok, BadRequest)
