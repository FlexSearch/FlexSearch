namespace FlexSearch.Core

open FlexSearch.Api
open FlexSearch.Api.Models
open FlexSearch.Api.Constants
open System.ComponentModel.Composition

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

# ws_PutIndexFieldsHandler """Update the Index Fields"""
# category "indices"
# description """
Any analyser which is to be used as part of an index field should be defined
before adding the field to the index.

<div class="note">
Always reindex the data after a field update, otherwise you may get unexpected
results.
</div>

<div class="important">
New fields added as part of fields update will not have any data available for
the older records, in such cases if the indexing is not done the engine will use
default values for the field type. If an existing field is removed then the data
associated with that field will not be accessible even though the data will not
be removed from the index itself.
</div>
"""
[<Name("PUT-/indices/:id/fields")>]
[<Sealed>]
type PutIndexFieldsHandler =
    inherit HttpHandlerBase<FieldsUpdateRequest, unit>

    new : indexService : IIndexService -> PutIndexFieldsHandler
    override Process : request : RequestContext * FieldsUpdateRequest option ->
        ResponseContext<unit>

[<Name("PUT-/indices/:id/searchprofile")>]
[<Sealed>]
type PutIndexSearchProfileHandler =
    inherit HttpHandlerBase<SearchQuery, unit>

    new : indexService : IIndexService -> PutIndexSearchProfileHandler
    override Process : request : RequestContext * SearchQuery option ->
        ResponseContext<unit>

[<Name("PUT-/indices/:id/configuration")>]
[<Sealed>]
type PutIndexConfigurationHandler =
    inherit HttpHandlerBase<IndexConfiguration, unit>

    new : indexService : IIndexService -> PutIndexConfigurationHandler
    override Process : request : RequestContext * IndexConfiguration option ->
        ResponseContext<unit>

type IndexStatusResponse =
    inherit DtoBase
    new : unit -> IndexStatusResponse
    #if prop_Status
    #endif
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

[<NameAttribute ("GET|POST-/indices/:id/search"); SealedAttribute ()>]
type GetSearchHandler =
    inherit Http.HttpHandlerBase<SearchQuery,obj>

    new : searchService:ISearchService -> GetSearchHandler
    override Process : request:Http.RequestContext * body:SearchQuery option ->
                Http.ResponseContext<obj>

// ----------------------------------------------------------------------------

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
