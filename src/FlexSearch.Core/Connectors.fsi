namespace FlexSearch.Core

#if dto_CsvIndexingRequest
Represents a request which can be sent to CSV connector to index CSV data.
#endif
[<Sealed>]
type CsvIndexingRequest =
    inherit DtoBase
    new : unit -> CsvIndexingRequest
    #if prop_IndexName
    Name of the index
    #endif
    member IndexName : string with get, set
    #if prop_HasHeaderRecord
    Signifies if the passed CSV file(s) has a header record 
    #endif
    member HasHeaderRecord : bool with get, set
    #if prop_Headers
    The headers to be used by each column. This should only be passed when there is
    no header in the csv file. The first column is always assumed to be id field. Make sure
    in your array you always offset the column names by 1 position.
    #endif
    member Headers : string array with get, set
    #if prop_Path
    The path of the folder or file to be indexed. The service will pickup all files with 
    .csv extension.
    #endif
    member Path : string with get, set

#if dto_SqlIndexingRequest
Represents a request which can be sent to Sql connector to index SQL data
#endif
[<Sealed>]
type SqlIndexingRequest = 
    inherit DtoBase
    new : unit -> SqlIndexingRequest
    override Validate : unit -> Choice<unit,IMessage>
    #if prop_IndexName
    Name of the index
    #endif
    member IndexName : string with get, set
    #if prop_Query
    The query to be used to fetch data from Sql server
    #endif
    member Query : string with get, set
    #if prop_ConnectionString
    Connection string used to connect to the server
    #endif
    member ConnectionString : string with get, set
    #if prop_ForceCreate
    Signifies if all updates to the index are create
    #endif
    member ForceCreate : bool with get, set
    #if prop_CreateJob
    Signifies if the connector should create a job for the task and return a jobId which can be used
    to check the status of the job.
    #endif
    member CreateJob : bool with get, set

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