namespace FlexSearch.DuplicateDetection

#if dto_DuplicateDetectionRequest
#endif
type DuplicateDetectionRequest = 
    inherit FlexSearch.Core.DtoBase
    new : unit -> DuplicateDetectionRequest
    #if prop_SelectionQuery
    #endif
    member SelectionQuery : string with get, set
    #if prop_DisplayName
    #endif
    member DisplayName : string with get, set
    #if prop_ThreadCount
    #endif
    member ThreadCount : int with get, set
    #if prop_IndexName
    #endif
    member IndexName : string with get, set
    #if prop_ProfileName
    #endif
    member ProfileName : string with get, set
    #if prop_MaxRecordsToScan
    #endif
    member MaxRecordsToScan : int16 with get, set
    #if prop_DuplicatesCount
    #endif
    member DuplicatesCount : int16 with get, set
    member NextId : FlexSearch.Core.AtomicLong

#if dto_DuplicateDetectionReportRequest
#endif
type DuplicateDetectionReportRequest = 
    inherit FlexSearch.Core.DtoBase
    new : unit -> DuplicateDetectionReportRequest
    #if prop_SourceFileName
    #endif
    member SourceFileName : string with get, set
    #if prop_ProfileName
    #endif
    member ProfileName : string with get, set
    #if prop_IndexName
    #endif
    member IndexName : string with get, set
    #if prop_QueryString
    #endif
    member QueryString : string with get, set
    #if prop_SelectionQuery
    #endif
    member SelectionQuery : string with get, set
    #if prop_CutOff
    #endif
    member CutOff : float with get, set

# ws_DuplicateDetectionHandler """
Duplicate Detection Handler
"""
[<SealedAttribute (); FlexSearch.Core.NameAttribute ("POST-/indices/:id/duplicatedetection/:id")>]
type DuplicateDetectionHandler =
    inherit FlexSearch.Core.Http.HttpHandlerBase<DuplicateDetectionRequest, System.Guid>
    new : indexService:FlexSearch.Core.IIndexService *
        documentService:FlexSearch.Core.IDocumentService *
        searchService:FlexSearch.Core.ISearchService -> DuplicateDetectionHandler
    override Process : request:FlexSearch.Core.Http.RequestContext * body:DuplicateDetectionRequest option ->
                FlexSearch.Core.Http.ResponseContext<System.Guid>

# ws_DuplicateDetectionReportHandler """
Duplicate Detection Report Handler
"""
[<SealedAttribute (); FlexSearch.Core.NameAttribute ("POST-/indices/:id/duplicatedetectionreport")>]
type DuplicateDetectionReportHandler =
    inherit FlexSearch.Core.Http.HttpHandlerBase<DuplicateDetectionReportRequest, System.Guid>
    new : indexService:FlexSearch.Core.IIndexService * searchService:FlexSearch.Core.ISearchService ->
            DuplicateDetectionReportHandler
    override Process : request:FlexSearch.Core.Http.RequestContext * body:DuplicateDetectionReportRequest option ->
                FlexSearch.Core.Http.ResponseContext<System.Guid>
