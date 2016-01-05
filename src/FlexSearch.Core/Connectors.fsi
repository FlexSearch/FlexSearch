namespace FlexSearch.Core

open FlexSearch.Api.Model
open System.ComponentModel.Composition

# ws_CsvHandler """
Connector for importing CSV file data into the system.
"""
[<SealedAttribute (); NameAttribute ("POST-/indices/:id/csv")>]
type CsvHandler =
    inherit Http.HttpHandlerBase<CsvIndexingRequest,string>

    new : queueService:IQueueService * indexService:IIndexService *
        jobService:IJobService -> CsvHandler
    override Process : request:Http.RequestContext * body:CsvIndexingRequest option ->
                Http.ResponseContext<string>

# ws_SqlHandler """
Connector for importing data from Microsoft SQL into the system.
"""
[<SealedAttribute (); NameAttribute ("POST-/indices/:id/sql")>]
type SqlHandler =
    inherit Http.HttpHandlerBase<SqlIndexingRequest,string>

    new : queueService:IQueueService * jobService:IJobService -> SqlHandler
    override Process : request:Http.RequestContext * body:SqlIndexingRequest option ->
                Http.ResponseContext<string>