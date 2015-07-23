namespace FlexSearch.Core
  
[<SealedAttribute (); NameAttribute ("GET-/ping")>]
type PingHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : unit -> PingHandler
    override Process : Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

[<NameAttribute ("GET-/"); SealedAttribute ()>]
type GetRootHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : unit -> GetRootHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

[<NameAttribute ("GET-/favicon.ico"); SealedAttribute ()>]
type GetFaviconHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : unit -> GetFaviconHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

[<NameAttribute ("GET-/indices"); SealedAttribute ()>]
type GetAllIndexHandler =
    inherit Http.HttpHandlerBase<NoBody,Index []>
    new : indexService:IIndexService -> GetAllIndexHandler
    override Process : Http.RequestContext * NoBody option ->
                Http.ResponseContext<Index []>

[<NameAttribute ("GET-/indices/:id"); SealedAttribute ()>]
type GetIndexByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,Index>
    new : indexService:IIndexService -> GetIndexByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<Index>

[<NameAttribute ("POST-/indices"); SealedAttribute ()>]
type PostIndexByIdHandler =
    inherit Http.HttpHandlerBase<Index,CreateResponse>
    new : indexService:IIndexService -> PostIndexByIdHandler
    override Process : Http.RequestContext * body:Index option ->
                Http.ResponseContext<CreateResponse>

[<NameAttribute ("DELETE-/indices/:id"); SealedAttribute ()>]
type DeleteIndexByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : indexService:IIndexService -> DeleteIndexByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

type IndexStatusResponse =
    new : unit -> IndexStatusResponse
    member Status : IndexStatus
    member Status : IndexStatus with set

[<NameAttribute ("GET-/indices/:id/status"); SealedAttribute ()>]
type GetStatusHandler =
    inherit Http.HttpHandlerBase<NoBody,IndexStatusResponse>
    new : indexService:IIndexService -> GetStatusHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<IndexStatusResponse>

[<NameAttribute ("PUT-/indices/:id/status/:id"); SealedAttribute ()>]
type PutStatusHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : indexService:IIndexService -> PutStatusHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

[<NameAttribute ("GET-/indices/:id/exists"); SealedAttribute ()>]
type GetExistsHandler =
    inherit Http.HttpHandlerBase<NoBody,IndexExistsResponse>
    new : indexService:IIndexService -> GetExistsHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<IndexExistsResponse>

[<NameAttribute ("GET-/indices/:id/size"); SealedAttribute ()>]
type GetIndexSizeHandler =
    inherit Http.HttpHandlerBase<NoBody,int64>
    new : indexService:IIndexService -> GetIndexSizeHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<int64>

[<NameAttribute ("GET-/analyzers/:id"); SealedAttribute ()>]
type GetAnalyzerByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,Analyzer>
    new : analyzerService:IAnalyzerService -> GetAnalyzerByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<Analyzer>

[<NameAttribute ("GET-/analyzers"); SealedAttribute ()>]
type GetAllAnalyzerHandler =
    inherit Http.HttpHandlerBase<NoBody,Analyzer []>
    new : analyzerService:IAnalyzerService -> GetAllAnalyzerHandler
    override Process : Http.RequestContext * NoBody option ->
                Http.ResponseContext<Analyzer []>

[<NameAttribute ("POST-/analyzers/:id/analyze"); SealedAttribute ()>]
type AnalyzeTextHandler =
    inherit Http.HttpHandlerBase<AnalysisRequest,string []>
    new : analyzerService:IAnalyzerService -> AnalyzeTextHandler
    override Process : request:Http.RequestContext * body:AnalysisRequest option ->
                Http.ResponseContext<string []>

[<NameAttribute ("DELETE-/analyzers/:id"); SealedAttribute ()>]
type DeleteAnalyzerByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : analyzerService:IAnalyzerService -> DeleteAnalyzerByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

[<NameAttribute ("PUT-/analyzers/:id"); SealedAttribute ()>]
type CreateOrUpdateAnalyzerByIdHandler =
    inherit Http.HttpHandlerBase<Analyzer,unit>
    new : analyzerService:IAnalyzerService ->
            CreateOrUpdateAnalyzerByIdHandler
    override Process : request:Http.RequestContext * body:Analyzer option ->
                Http.ResponseContext<unit>

[<NameAttribute ("GET-/indices/:id/documents"); SealedAttribute ()>]
type GetDocumentsHandler =
    inherit Http.HttpHandlerBase<NoBody,SearchResults>
    new : documentService:IDocumentService -> GetDocumentsHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<SearchResults>

[<NameAttribute ("GET-/indices/:id/documents/:id"); SealedAttribute ()>]
type GetDocumentByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,Document>
    new : documentService:IDocumentService -> GetDocumentByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<Document>

[<NameAttribute ("POST-/indices/:id/documents"); SealedAttribute ()>]
type PostDocumentByIdHandler =
    inherit Http.HttpHandlerBase<Document,CreateResponse>
    new : documentService:IDocumentService -> PostDocumentByIdHandler
    override Process : Http.RequestContext * body:Document option ->
                Http.ResponseContext<CreateResponse>

[<NameAttribute ("DELETE-/indices/:id/documents"); SealedAttribute ()>]
type DeleteDocumentsHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : documentService:IDocumentService -> DeleteDocumentsHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

[<NameAttribute ("DELETE-/indices/:id/documents/:id"); SealedAttribute ()>]
type DeleteDocumentByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : documentService:IDocumentService -> DeleteDocumentByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

[<NameAttribute ("PUT-/indices/:id/documents/:id"); SealedAttribute ()>]
type PutDocumentByIdHandler =
    inherit Http.HttpHandlerBase<Document,unit>
    new : documentService:IDocumentService -> PutDocumentByIdHandler
    override Process : Http.RequestContext * body:Document option ->
                Http.ResponseContext<unit>

[<NameAttribute ("PUT-/setupdemo"); SealedAttribute ()>]
type SetupDemoHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : service:DemoIndexService -> SetupDemoHandler
    override Process : Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

[<NameAttribute ("GET-/jobs/:id"); SealedAttribute ()>]
type GetJobByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,Job>
    new : jobService:IJobService -> GetJobByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<Job>

// ----------------------------------------------------------------------------

# ws_GetSearchHandler """
Search across the index for documents using SQL like query syntax.
{{note: Any parameter passed as part of query string takes precedence over the same parameter in the request body.}}"""
# meth "GET|POST"
# uri "/indices/:id/search"
# param_q """Short hand for 'QueryString'."""
# param_count """Count parameter. Refer to 'Search Query' properties."""
# param_skip """Skip parameter. Refer to 'Search Query' properties."""
# param_orderby """Order by parameter. Refer to 'Search Query' properties."""
# param_orderbydirection """Order by Direction parameter. Refer to 'Search Query' properties."""
# param_returnflatresult """Return flat results parameter. Refer to 'Search Query' properties."""
# description """
---
title: Search APIs
layout: docs.html
---

FlexSearch follows a consistent search dsl to execute all kind of search request. 
This enables a unified search experience for the developers. 

## What can you do with Search Query?

The FlexSearch API lets you do the following with the search endpoint:

{{> 'resourcelist' resource-search}}

Before getting into the various types of search queries supported by FlexSearch, 
we will cover the basic search mechanics.

## Properties
{{> 'properties' searchquery}}
"""
# examples """
data/rest-examples/post-indices-search-fuzzy-1.json
data/rest-examples/post-indices-search-fuzzy-2.json
data/rest-examples/post-indices-search-fuzzy-3.json
"""
[<NameAttribute ("GET|POST-/indices/:id/search"); SealedAttribute ()>]
type GetSearchHandler =
    inherit Http.HttpHandlerBase<SearchQuery,obj>
    new : searchService:ISearchService -> GetSearchHandler
    override Process : request:Http.RequestContext * body:SearchQuery option ->
                Http.ResponseContext<obj>

// ----------------------------------------------------------------------------

# ws_DeleteDocumentsFromSearchHandler """
Deletes all document returned by the search query for the given index. Returns the records identified
by the search query."""
# meth "DELETE"
# uri "/indices/:indexId/search"
# param_q """Short hand for 'QueryString'."""
# param_count """Count parameter. Refer to 'Search Query' properties."""
# param_skip """Skip parameter. Refer to 'Search Query' properties."""
# param_orderby """Order by parameter. Refer to 'Search Query' properties."""
# param_orderbydirection """Order by Direction parameter. Refer to 'Search Query' properties."""
# description """
## TODO
"""
# examples """"""
[<NameAttribute ("DELETE-/indices/:id/search"); SealedAttribute ()>]
type DeleteDocumentsFromSearchHandler =
    inherit Http.HttpHandlerBase<NoBody,obj>
    new : documentService:IDocumentService -> DeleteDocumentsFromSearchHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<obj>

[<NameAttribute ("POST-/indices/:id/searchprofiletest"); SealedAttribute ()>]
type PostSearchProfileTestHandler =
    inherit Http.HttpHandlerBase<SearchProfileTestDto,obj>
    new : searchService:ISearchService -> PostSearchProfileTestHandler
    override Process : request:Http.RequestContext * body:SearchProfileTestDto option ->
                Http.ResponseContext<obj>

[<NameAttribute ("GET-/memory"); SealedAttribute ()>]
type GetMemoryDetails =
    inherit Http.HttpHandlerBase<NoBody,MemoryDetailsResponse>
    new : unit -> GetMemoryDetails
    override Process : request:Http.RequestContext * body:NoBody option ->
                Http.ResponseContext<MemoryDetailsResponse>
