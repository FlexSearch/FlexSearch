namespace FlexSearch.DuplicateDetection
open System
open System.Collections.Generic

[<Sealed>]
type Session = 
    member Id : string with get
    member SessionId : string with get
    member IndexName: string with get, set
    member ProfileName: string with get, set
    member JobStartTime : DateTime with get, set
    member JobEndTime : DateTime with get, set
    member SelectionQuery: string with get, set
    member DisplayFieldName: string with get, set
    member RecordsReturned : int with get, set
    member RecordsAvailable : int with get, set
    member ThreadCount : int with get, set

type TargetRecord =
    new : unit -> TargetRecord 
    member TargetId : int with get, set
    member TargetRecordId : string with get, set
    member TargetDisplayName : string with get, set
    member TrueDuplicate : bool with get, set
    member Quality : string with get, set
    member TargetScore : float32 with get, set

type SourceRecord = 
    new : string -> SourceRecord
    member SessionId : string with get
    member SourceId : int with get, set
    member SourceRecordId : string with get, set
    member SourceDisplayName : string with get, set
    member SourceStatus : string with get, set
    member TotalDupes: int with get, set
    member TargetRecords : TargetRecord [] with get, set

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