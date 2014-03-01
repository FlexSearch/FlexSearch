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
namespace FlexSearch.Core.HttpModule

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Core.State
open FlexSearch.Utility
open Microsoft.Owin
open Newtonsoft.Json
open Owin
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.Composition
open System.Linq
open System.Net
open System.Net.Http

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "index")>]
type IndexModule() = 
    inherit HttpModuleBase()
    override this.Get(indexName, owin, state) = 
        owin |> responseProcessor (state.IndexService.GetIndex(indexName)) OK BAD_REQUEST
    
    override this.Post(indexName, owin, state) = 
        match getRequestBody<Index> (owin.Request) with
        | Choice1Of2(index) -> 
            // Index name passed in url taskes precedence
            index.IndexName <- indexName
            owin |> responseProcessor (state.IndexService.AddIndex(index)) OK BAD_REQUEST
        | Choice2Of2(error) -> owin |> BAD_REQUEST error
    
    override this.Delete(indexName, owin, state) = 
        owin |> responseProcessor (state.IndexService.DeleteIndex(indexName)) OK BAD_REQUEST
    override this.Put(indexName, owin, state) = 
        match getRequestBody<Index> (owin.Request) with
        | Choice1Of2(index) -> 
            // Index name passed in url taskes precedence
            index.IndexName <- indexName
            owin |> responseProcessor (state.IndexService.UpdateIndex(index)) OK BAD_REQUEST
        | Choice2Of2(error) -> owin |> BAD_REQUEST error

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "documents")>]
type DocumentModule() = 
    inherit HttpModuleBase()
    
    override this.Get(indexName, owin, state) = 
        let processRequest = 
            maybe { 
                match checkIdPresent (owin) with
                | Some(id) -> 
                    // documents/{id}
                    // Return the requested document
                    return! state.IndexService.PerformQuery
                                (indexName, new SearchQuery(indexName, (sprintf "%s = '%s'" Constants.IdField id)))
                | None -> 
                    // documents
                    // Return top 10 documents
                    let q = new SearchQuery(indexName, (sprintf "%s matchall 'x'" Constants.IdField))
                    q.Columns.Add("*")
                    q.MissingValueConfiguration.Add(Constants.IdField, MissingValueOption.Ignore)
                    return! state.IndexService.PerformQuery(indexName, q)
            }
        owin |> responseProcessor processRequest OK BAD_REQUEST
    
    override this.Post(indexName, owin, state) = 
        let processRequest = 
            maybe { 
                match checkIdPresent (owin) with
                | Some(id) -> 
                    // documents/{id}
                    // Add the document by id
                    let! fields = getRequestBody<Dictionary<string, string>> (owin.Request)
                    match fields.TryGetValue(Constants.IdField) with
                    | true, _ -> 
                        // Overide dictinary id with the url id
                        fields.[Constants.IdField] <- id
                    | _ -> fields.Add(Constants.IdField, id)
                    return! state.IndexService.PerformCommand(indexName, IndexCommand.Create(id, fields))
                | None -> 
                    // documents
                    // Bulk addition of the documents
                    return! Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
            }
        owin |> responseProcessor processRequest OK BAD_REQUEST
    
    override this.Delete(indexName, owin, state) = 
        let processRequest = 
            maybe { 
                match checkIdPresent (owin) with
                | Some(id) -> 
                    // documents/{id}
                    // Delete the document by id
                    return! state.IndexService.PerformCommand(indexName, IndexCommand.Delete(id))
                | None -> 
                    // documents
                    // Bulk delete of the documents
                    return! Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
            }
        owin |> responseProcessor processRequest OK BAD_REQUEST
    
    override this.Put(indexName, owin, state) = 
        let processRequest = 
            maybe { 
                match checkIdPresent (owin) with
                | Some(id) -> 
                    // documents/{id}
                    // Add or update the document by id
                    let! fields = getRequestBody<Dictionary<string, string>> (owin.Request)
                    match fields.TryGetValue(Constants.IdField) with
                    | true, _ -> 
                        // Overide dictinary id with the url id
                        fields.[Constants.IdField] <- id
                    | _ -> fields.Add(Constants.IdField, id)
                    return! state.IndexService.PerformCommand(indexName, IndexCommand.Update(id, fields))
                | None -> 
                    // documents
                    // Bulk addition of the documents
                    return! Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
            }
        owin |> responseProcessor processRequest OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "search")>]
type SearchModule() = 
    inherit HttpModuleBase()
    
    let processRequest (indexName, owin : IOwinContext, state : NodeState) = 
        maybe { 
            let query = 
                match getRequestBody<SearchQuery> (owin.Request) with
                | Choice1Of2(q) -> q
                | Choice2Of2(_) -> 
                    // It is possible that the query is supplied through querystring
                    new SearchQuery()
            query.QueryString <- getValueFromQueryString "q" query.QueryString owin
            query.Columns <- match owin.Request.Query.Get("c") with
                             | null -> query.Columns
                             | v -> v.Split([| ',' |], System.StringSplitOptions.RemoveEmptyEntries).ToList()
            query.Count <- getIntValueFromQueryString "count" query.Count owin
            query.Skip <- getIntValueFromQueryString "skip" query.Skip owin
            query.OrderBy <- getValueFromQueryString "orderby" query.OrderBy owin
            query.ReturnFlatResult <- getBoolValueFromQueryString "returnflatresult" query.ReturnFlatResult owin
            query.IndexName <- indexName
            match state.IndexService.PerformQuery(indexName, query) with
            | Choice1Of2(v') -> 
                if query.ReturnFlatResult then 
                    owin.Response.Headers.Add("RecordsReturned", [| v'.RecordsReturned.ToString() |])
                    owin.Response.Headers.Add("TotalAvailable", [| v'.TotalAvailable.ToString() |])
                    let result = v'.Documents |> Seq.map (fun x -> x.Fields)
                    return! Choice1Of2(result :> obj)
                else return! Choice1Of2(v' :> obj)
            | Choice2Of2(e) -> return! Choice2Of2(e)
        }
    
    override this.Get(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
    override this.Post(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "exists")>]
type ExistsModule() = 
    inherit HttpModuleBase()
    
    let processRequest (indexName : string, owin : IOwinContext, state : NodeState) = 
        match state.IndexService.IndexStatus(indexName) with
        | Choice1Of2(_) -> Choice1Of2()
        | Choice2Of2(e) -> Choice2Of2(e)
    
    override this.Get(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
    override this.Post(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "status")>]
type StatusModule() = 
    inherit HttpModuleBase()
    
    override this.Get(indexName, owin, state) = 
        let processRequest = 
            match state.IndexService.IndexStatus(indexName) with
            | Choice1Of2(status) -> Choice1Of2(new IndexStatus(status))
            | Choice2Of2(e) -> Choice2Of2(e)
        owin |> responseProcessor processRequest OK BAD_REQUEST
    
    override this.Post(indexName, owin, state) = 
        let processRequest = 
            match checkIdPresent (owin) with
            | Some(id) -> 
                match id with
                | InvariantEqual "online" -> state.IndexService.OpenIndex(indexName)
                | InvariantEqual "offline" -> state.IndexService.CloseIndex(indexName)
                | _ -> Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
            | None -> Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
        owin |> responseProcessor processRequest OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "analysis")>]
type AnalysisModule() = 
    inherit HttpModuleBase()
    let processRequest (indexName, owin, state : NodeState) = maybe { let! index = state.IndexService.GetIndex
                                                                                       (indexName)
                                                                      return! Choice1Of2() }
    override this.Get(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
    override this.Post(indexName, owin, state) = 
        owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST

[<Export(typeof<HttpModuleBase>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "/")>]
type RootModule() = 
    inherit HttpModuleBase()
    override this.Get(indexName, owin, state) = 
        owin.Response.ContentType <- "text/html"
        owin.Response.StatusCode <- 200
        await 
            (owin.Response.WriteAsync
                 ("FlexSearch " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()))
