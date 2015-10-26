namespace FlexSearch.Core
 
# ws_PingHandler """Ping server"""
# category "server"
# description """
A simple endpoint which can be used to check the server is running. This is
useful for checking the status of the server from a load balancer or fire-wall.
"""
[<SealedAttribute (); NameAttribute ("GET-/ping")>]
type PingHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : unit -> PingHandler
    override Process : Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

# ws_GetRootHandler """Redirect requests from base URL"""
# category "server"
# description """
An internal endpoint which is used to redirect requests to the root URL to the
FlexSearch portal.
"""
[<NameAttribute ("GET-/"); SealedAttribute ()>]
type GetRootHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : unit -> GetRootHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

# ws_GetFaviconHandler """Returns favourite icon"""
# category "server"
# description """
An internal end point which is used to return favourite icon when it is
requested by a web browser.
"""
[<NameAttribute ("GET-/favicon.ico"); SealedAttribute ()>]
type GetFaviconHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : unit -> GetFaviconHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

# ws_GetAllIndexHandler """Returns all the indexes"""
# category "indices"
[<NameAttribute ("GET-/indices"); SealedAttribute ()>]
type GetAllIndexHandler =
    inherit Http.HttpHandlerBase<NoBody,Index []>
    new : indexService:IIndexService -> GetAllIndexHandler
    override Process : Http.RequestContext * NoBody option ->
                Http.ResponseContext<Index []>

# ws_GetIndexByIdHandler """Returns an index by the ID"""
# category "indices"
# description """
This service will return a status of 404 when index is not present on the server.
"""
[<NameAttribute ("GET-/indices/:id"); SealedAttribute ()>]
type GetIndexByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,Index>
    new : indexService:IIndexService -> GetIndexByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<Index>

# ws_PostIndexByIdHandler """Create a new index"""
[<NameAttribute ("POST-/indices"); SealedAttribute ()>]
type PostIndexByIdHandler =
    inherit Http.HttpHandlerBase<Index,CreateResponse>
    new : indexService:IIndexService -> PostIndexByIdHandler
    override Process : Http.RequestContext * body:Index option ->
                Http.ResponseContext<CreateResponse>

# ws_DeleteIndexByIdHandler """Deletes an index by ID"""
# category "indices"
# description """
Index deletion happens in two parts, first the index configuration file is
deleted from the configurations folder, then the index is deleted from the data
folder. In case any error is encountered the cleanup will be performed on the
server restart.
"""
[<NameAttribute ("DELETE-/indices/:id"); SealedAttribute ()>]
type DeleteIndexByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : indexService:IIndexService -> DeleteIndexByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

#if dto_FieldsUpdateRequest
Container to store the list of fields to be updated
#endif
type FieldsUpdateRequest =
    inherit DtoBase
    new : unit -> FieldsUpdateRequest
    #if prop_Fields
    #endif
    member Fields : Field [] with get, set

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

# ws_PutIndexSearchProfileHandler """Adds or updates a search profile for the given index"""
# category "indices"
[<Name("PUT-/indices/:id/searchprofile")>]
[<Sealed>]
type PutIndexSearchProfileHandler =
    inherit HttpHandlerBase<SearchQuery, unit>
    new : indexService : IIndexService -> PutIndexSearchProfileHandler
    override Process : request : RequestContext * SearchQuery option ->
        ResponseContext<unit>

# ws_PutIndexConfigurationHandler """
Update the configuration of an index"""
# category "indices"
# description """
<div class="important">
The Index Version cannot be modified
</div>
"""
[<Name("PUT-/indices/:id/configuration")>]
[<Sealed>]
type PutIndexConfigurationHandler =
    inherit HttpHandlerBase<IndexConfiguration, unit>
    new : indexService : IIndexService -> PutIndexConfigurationHandler
    override Process : request : RequestContext * IndexConfiguration option ->
        ResponseContext<unit>

#if dto_IndexStatusResponse
#endif
type IndexStatusResponse =
    inherit DtoBase
    new : unit -> IndexStatusResponse
    #if prop_Status
    #endif
    member Status : IndexStatus
    member Status : IndexStatus with set

# ws_GetStatusHandler """Returns the status of an index"""
# category "indices"
# description """
This endpoint can be used to determine if an index is online or off-line.
"""
[<NameAttribute ("GET-/indices/:id/status"); SealedAttribute ()>]
type GetStatusHandler =
    inherit Http.HttpHandlerBase<NoBody,IndexStatusResponse>
    new : indexService:IIndexService -> GetStatusHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<IndexStatusResponse>

# ws_PutStatusHandler """Update the status of an index"""
# category "indices"
# description """
This endpoint can be used to set an index online or off-line.
"""
[<NameAttribute ("PUT-/indices/:id/status/:id"); SealedAttribute ()>]
type PutStatusHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : indexService:IIndexService -> PutStatusHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

# ws_GetExistsHandler """Check if an index exists"""
# category "indices"
# description """
This endpoint can be used to check if an index is present in the system. This
endpoint is a lighter alternative to accessing the index by an ID as the
response is smaller in size.
"""
[<NameAttribute ("GET-/indices/:id/exists"); SealedAttribute ()>]
type GetExistsHandler =
    inherit Http.HttpHandlerBase<NoBody,IndexExistsResponse>
    new : indexService:IIndexService -> GetExistsHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<IndexExistsResponse>

# ws_GetIndexSizeHandler """Returns the size of an index"""
# category "indices"
# description """
The return size may be higher than the actual size of the documents present in
the index. The return value includes the space occupied by the transaction logs
and older segment files which are not cleaned up as part of the last comment.
"""
[<NameAttribute ("GET-/indices/:id/size"); SealedAttribute ()>]
type GetIndexSizeHandler =
    inherit Http.HttpHandlerBase<NoBody,int64>
    new : indexService:IIndexService -> GetIndexSizeHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<int64>

# ws_GetAnalyzerByIdHandler """Returns an analyser by ID"""
# category "analyzer"
[<NameAttribute ("GET-/analyzers/:id"); SealedAttribute ()>]
type GetAnalyzerByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,Analyzer>
    new : analyzerService:IAnalyzerService -> GetAnalyzerByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<Analyzer>

# ws_GetAllAnalyzerHandler """Returns all analysers"""
# category "analyzer"
[<NameAttribute ("GET-/analyzers"); SealedAttribute ()>]
type GetAllAnalyzerHandler =
    inherit Http.HttpHandlerBase<NoBody,Analyzer []>
    new : analyzerService:IAnalyzerService -> GetAllAnalyzerHandler
    override Process : Http.RequestContext * NoBody option ->
                Http.ResponseContext<Analyzer []>

# ws_AnalyzeTextHandler """Analyse input next"""
# category "analyzer"
# description """
This endpoint is useful to understand the effect of a particular analyser on
the input text. You can use the service with both custom and built-in analysers.
The returned response contains the tokenised input.
"""
[<NameAttribute ("POST-/analyzers/:id/analyze"); SealedAttribute ()>]
type AnalyzeTextHandler =
    inherit Http.HttpHandlerBase<AnalysisRequest,string []>
    new : analyzerService:IAnalyzerService -> AnalyzeTextHandler
    override Process : request:Http.RequestContext * body:AnalysisRequest option ->
                Http.ResponseContext<string []>

# ws_DeleteAnalyzerByIdHandler """Deletes an analyser by ID"""
# category "analyzer"
[<NameAttribute ("DELETE-/analyzers/:id"); SealedAttribute ()>]
type DeleteAnalyzerByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : analyzerService:IAnalyzerService -> DeleteAnalyzerByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

# ws_CreateOrUpdateAnalyzerByIdHandler """Create or update an analyser"""
# category "analyzer"
[<NameAttribute ("PUT-/analyzers/:id"); SealedAttribute ()>]
type CreateOrUpdateAnalyzerByIdHandler =
    inherit Http.HttpHandlerBase<Analyzer,unit>
    new : analyzerService:IAnalyzerService ->
            CreateOrUpdateAnalyzerByIdHandler
    override Process : request:Http.RequestContext * body:Analyzer option ->
                Http.ResponseContext<unit>

# ws_GetDocumentsHandler """Returns top 10 document from the index"""
# category "documents"
# description """
This endpoint is useful to determine the structure of the documents indexed. At
times it is quicker to get the count of all the documents present in the index
using the service rather then using the search API.
"""
[<NameAttribute ("GET-/indices/:id/documents"); SealedAttribute ()>]
type GetDocumentsHandler =
    inherit Http.HttpHandlerBase<NoBody,SearchResults>
    new : documentService:IDocumentService -> GetDocumentsHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<SearchResults>

# ws_GetDocumentByIdHandler """Returns document by ID"""
# category "documents"
[<NameAttribute ("GET-/indices/:id/documents/:id"); SealedAttribute ()>]
type GetDocumentByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,Document>
    new : documentService:IDocumentService -> GetDocumentByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<Document>

# ws_PostDocumentByIdHandler """Creates a new document"""
# category "documents"
# description """
Unlike a database system FlexSearch doesn't impose the requirement of a unique
ID per document. You can add multiple documents by the same ID but this can
impose a problem while adding or retrieving them. You can enforce a unique ID
check by using the `timestamp` field. To understand more about ID check and
concurrency control, please refer to the article `concurrency control` under
concepts section.
"""
[<NameAttribute ("POST-/indices/:id/documents"); SealedAttribute ()>]
type PostDocumentByIdHandler =
    inherit Http.HttpHandlerBase<Document,CreateResponse>
    new : documentService:IDocumentService -> PostDocumentByIdHandler
    override Process : Http.RequestContext * body:Document option ->
                Http.ResponseContext<CreateResponse>

# ws_DeleteDocumentsHandler """Deletes all documents present in the index"""
# category "documents"
# description """
This will remove all the documents present in an index. This is useful when you
want to reindex all the documents.
"""
[<NameAttribute ("DELETE-/indices/:id/documents"); SealedAttribute ()>]
type DeleteDocumentsHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : documentService:IDocumentService -> DeleteDocumentsHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

# ws_DeleteDocumentByIdHandler """Delete a document by ID"""
# category "documents"
# description """
In case of non-unique ID field, this will delete all the documents associated
with that ID.
"""
[<NameAttribute ("DELETE-/indices/:id/documents/:id"); SealedAttribute ()>]
type DeleteDocumentByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : documentService:IDocumentService -> DeleteDocumentByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

# ws_PutDocumentByIdHandler """Create or update a document"""
# category "documents"
# description """
It is advisable to use create document endpoint when you are sure that the
document does not exist in an index. This service will always perform an ID
based look up to determine if a document already exists. In case of non-unique
ID based index, this will replace all the documents with the currently passed
document. This endpoint can be used with concurrency control semantics.
"""
[<NameAttribute ("PUT-/indices/:id/documents/:id"); SealedAttribute ()>]
type PutDocumentByIdHandler =
    inherit Http.HttpHandlerBase<Document,unit>
    new : documentService:IDocumentService -> PutDocumentByIdHandler
    override Process : Http.RequestContext * body:Document option ->
                Http.ResponseContext<unit>

# ws_SetupDemoHandler """Setup a demo index"""
# category "server"
# description """
This endpoint if useful for setting up a demo index which can be used to explore
the capabilities of FlexSearch. This is an in memory index which gets wiped out
on server restart.
"""
[<NameAttribute ("PUT-/setupdemo"); SealedAttribute ()>]
type SetupDemoHandler =
    inherit Http.HttpHandlerBase<NoBody,unit>
    new : service:DemoIndexService -> SetupDemoHandler
    override Process : Http.RequestContext * NoBody option ->
                Http.ResponseContext<unit>

# ws_GetJobByIdHandler """Returns job information"""
# category "jobs"
[<NameAttribute ("GET-/jobs/:id"); SealedAttribute ()>]
type GetJobByIdHandler =
    inherit Http.HttpHandlerBase<NoBody,Job>
    new : jobService:IJobService -> GetJobByIdHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<Job>

// ----------------------------------------------------------------------------

# ws_GetSearchHandler """Search and index"""
# category "search"
# param_q """Short hand for 'QueryString'."""
# param_c """Columns to be retrieved. Use * to retrieve all columns."""
# param_count """Count parameter. Refer to 'Search Query' properties."""
# param_skip """Skip parameter. Refer to 'Search Query' properties."""
# param_orderby """Order by parameter. Refer to 'Search Query' properties."""
# param_orderbydirection """Order by Direction parameter. Refer to 'Search Query' properties."""
# param_returnflatresult """Return flat results parameter. Refer to 'Search Query' properties."""
# uriparam_id """Name of the FlexSearch index"""
# description """
Search across the index for documents using SQL like query syntax.

<div class= "note">
Any parameter passed as part of query string takes precedence over the same
parameter in the request body.
</div>

Refer to the search DSL section to learn more about FlexSearch's querying capability.
"""

# examples """
post-indices-search-fuzzy-1
post-indices-search-fuzzy-2
post-indices-search-fuzzy-3
"""
[<NameAttribute ("GET|POST-/indices/:id/search"); SealedAttribute ()>]
type GetSearchHandler =
    inherit Http.HttpHandlerBase<SearchQuery,obj>
    new : searchService:ISearchService -> GetSearchHandler
    override Process : request:Http.RequestContext * body:SearchQuery option ->
                Http.ResponseContext<obj>

// ----------------------------------------------------------------------------

# ws_GetFacetedSearchHandler """
"""
[<Name("POST-/indices/:id/facetedsearch")>]
[<Sealed>]
type GetFacetedSearchHandler =
    inherit Http.HttpHandlerBase<FacetQuery, Group []>
    new : searchService:ISearchService -> GetFacetedSearchHandler
    override Process : request:Http.RequestContext * body:FacetQuery option ->
                Http.ResponseContext<Group []>
    

# ws_DeleteDocumentsFromSearchHandler """
Deletes all document returned by the search query for the given index. Returns the records identified
by the search query."""
# param_q """Short hand for 'QueryString'."""
# param_count """Count parameter. Refer to 'Search Query' properties."""
# param_skip """Skip parameter. Refer to 'Search Query' properties."""
# param_orderby """Order by parameter. Refer to 'Search Query' properties."""
# param_orderbydirection """Order by Direction parameter. Refer to 'Search Query' properties."""
# uriparam_id """Name of the FlexSearch index"""
# description """
Deletes all document returned by the search query for the given index. Returns the records identified
by the search query."""
[<NameAttribute ("DELETE-/indices/:id/search"); SealedAttribute ()>]
type DeleteDocumentsFromSearchHandler =
    inherit Http.HttpHandlerBase<NoBody,obj>
    new : documentService:IDocumentService -> DeleteDocumentsFromSearchHandler
    override Process : request:Http.RequestContext * NoBody option ->
                Http.ResponseContext<obj>

# ws_PostSearchProfileTestHandler """Test a search profile"""
# category "search"
# description """
This endpoint is useful to test such profiles dynamically, you can test search
profiles without adding them to the index. This becomes useful when trying out
different search profiles. It is advisable to not to use this service directly
but through the search UI provided as part of the portal.
"""
[<NameAttribute ("POST-/indices/:id/searchprofiletest"); SealedAttribute ()>]
type PostSearchProfileTestHandler =
    inherit Http.HttpHandlerBase<SearchProfileTestDto,obj>
    new : searchService:ISearchService -> PostSearchProfileTestHandler
    override Process : request:Http.RequestContext * body:SearchProfileTestDto option ->
                Http.ResponseContext<obj>

# ws_GetMemoryDetails """Returns memory used by the server"""
# category "server"
[<NameAttribute ("GET-/memory"); SealedAttribute ()>]
type GetMemoryDetails =
    inherit Http.HttpHandlerBase<NoBody,MemoryDetailsResponse>
    new : unit -> GetMemoryDetails
    override Process : request:Http.RequestContext * body:NoBody option ->
                Http.ResponseContext<MemoryDetailsResponse>
