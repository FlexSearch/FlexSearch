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
open Microsoft.Owin
open System.Linq

/// <summary>
///  Search for documents
/// </summary>
/// <remarks>
/// Search across the index for documents using SQL like query syntax.
/// {{note: Any parameter passed as part of query string takes precedence over the same parameter in the request body.}}
/// </remarks>
/// <parameters>
/// <parameter name="q" required="true">Short hand for 'QueryString'.</parameter>
/// <parameter name="c" required="false">Short hand for 'Columns'.</parameter>
/// <parameter name="count">Count parameter. Refer to 'Search Query' properties.</parameter>
/// <parameter name="skip">Skip parameter. Refer to 'Search Query' properties.</parameter>
/// <parameter name="orderby">Order by parameter. Refer to 'Search Query' properties.</parameter>
/// <parameter name="returnflatresult">Return flat results parameter. Refer to 'Search Query' properties.</parameter>
/// </parameters>
/// <method>GET|POST</method>
/// <uri>/indices/:id/search</uri>
/// <resource>search</resource>
/// <id>search-an-index</id>
[<Name("GET|POST-/indices/:id/search")>]
[<Sealed>]
type GetSearchHandler(searchService : ISearchService) = 
    inherit HttpHandlerBase<SearchQuery, obj>(false)
    override this.Process(id, subId, body, context) = 
        let processRequest (indexName, owin : IOwinContext) = 
            maybe { 
                let query = 
                    match body with
                    | Some(q) -> q
                    | None -> new SearchQuery()
                query.QueryString <- GetValueFromQueryString "q" query.QueryString owin
                query.Columns <- match owin.Request.Query.Get("c") with
                                 | null -> query.Columns
                                 | v -> v.Split([| ',' |], System.StringSplitOptions.RemoveEmptyEntries).ToList()
                query.Count <- GetIntValueFromQueryString "count" query.Count owin
                query.Skip <- GetIntValueFromQueryString "skip" query.Skip owin
                query.OrderBy <- GetValueFromQueryString "orderby" query.OrderBy owin
                query.ReturnFlatResult <- GetBoolValueFromQueryString "returnflatresult" query.ReturnFlatResult owin
                query.SearchProfile <- GetValueFromQueryString "searchprofile" "" owin
                query.IndexName <- indexName
                if query.ReturnFlatResult then 
                    let! (results, recordsReturned, totalAvailable) = searchService.SearchAsDictionarySeq(query)
                    owin.Response.Headers.Add("RecordsReturned", [| recordsReturned.ToString() |])
                    owin.Response.Headers.Add("TotalAvailable", [| totalAvailable.ToString() |])
                    return! Choice1Of2(results.ToList() :> obj)
                else 
                    match searchService.Search(query) with
                    | Choice1Of2(v') -> return! Choice1Of2(v' :> obj)
                    | Choice2Of2(e) -> return! Choice2Of2(e)
            }
        (processRequest (id.Value, context), Ok, BadRequest)
