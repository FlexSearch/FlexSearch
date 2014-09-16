namespace FlexSearch.Client

open FlexSearch.Api
open FlexSearch.Api.Errors
open FlexSearch.Api.Messages
open System
open System.Collections.Generic
open System.Net
open System.Net.Http
open System.Net.Http.Formatting
open System.Text
open System.Threading.Tasks

type IIndexServiceClient = 
    abstract Connect : unit -> Task<Response<unit>>
    abstract AddIndex : index:Index -> Task<Response<CreateResponse>>
    abstract UpdateIndex : index:Index -> Task<Response<unit>>
    abstract DeleteIndex : indexName:string -> Task<Response<unit>>
    abstract GetIndex : indexName:string -> Task<Response<Index>>
    abstract GetIndexStatus : indexName:string -> Task<Response<IndexStatusResponse>>
    abstract BringIndexOnline : indexName:string -> Task<Response<unit>>
    abstract SetIndexOffline : indexName:string -> Task<Response<unit>>
    abstract GetAllIndex : unit -> Task<Response<List<Index>>>
    abstract IndexExists : indexName:string -> Task<Response<IndexExistsResponse>>

type IDocumentServiceClient = 
    abstract GetTopDocuments : indexName:string * count:int -> Task<Response<SearchResults>>
    abstract AddDocument : indexName:string * document:FlexDocument -> Task<Response<CreateResponse>>
    abstract UpdateDocument : indexName:string * document:FlexDocument -> Task<Response<unit>>
    abstract DeleteDocument : indexName:string * id:string -> Task<Response<unit>>
    abstract GetDocument : indexName:string * id:string -> Task<Response<ResultDocument>>

type ISearchServiceClient = 
    abstract Search : searchRequest:SearchQuery -> Task<Response<SearchResults>>

type IFlexClient = 
    inherit IIndexServiceClient
    inherit IDocumentServiceClient
    inherit ISearchServiceClient

/// <summary>
/// Simple request logging handler. This is not thread safe. Only use
/// for testing
/// </summary>
type LoggingHandler(innerHandler : HttpMessageHandler) = 
    inherit DelegatingHandler(innerHandler)
    let log = new StringBuilder()
    let mutable statusCode = System.Net.HttpStatusCode.OK
    member this.Log() = log
    member this.StatusCode() = statusCode
    override this.SendAsync(request : HttpRequestMessage, cancellationToken) = 
        let log (work : Task<HttpResponseMessage>) = 
            async { 
                log.AppendLine("Request") |> ignore
                log.Append(request.Method.ToString() + " ") |> ignore
                log.AppendLine(request.RequestUri.ToString()) |> ignore
                // Add body here
                if request.Content <> null then let! requestBody = Async.AwaitTask(request.Content.ReadAsStringAsync())
                                                log.AppendLine(requestBody) |> ignore
                log.AppendLine("----------------------------------") |> ignore
                log.AppendLine("Response") |> ignore
                let! response = Async.AwaitTask(work)
                statusCode <- response.StatusCode
                let! responseBody = Async.AwaitTask(response.Content.ReadAsStringAsync())
                log.AppendLine(responseBody) |> ignore
                return response
            }
        
        let work = base.SendAsync(request, cancellationToken)
        Async.StartAsTask(log (work))

type FlexClient(uri : Uri, httpClient : HttpClient, ?defaultConnectionLimit : int) = 
    let mutable client = httpClient
    let connectionLimit = defaultArg defaultConnectionLimit 200
    let mediaTypeFormatter = new JsonMediaTypeFormatter()
    let mediaTypeFormatters = [ new JsonMediaTypeFormatter() :> MediaTypeFormatter ]
    
    do 
        if client = Unchecked.defaultof<_> then 
            client <- new HttpClient()
            client.BaseAddress <- uri
        else client.BaseAddress <- uri
        ServicePointManager.DefaultConnectionLimit <- connectionLimit
    
    new(uri : Uri) = FlexClient(uri, Unchecked.defaultof<HttpClient>, 200)
    new(uri : Uri, defaultConnectionLimit : int) = 
        FlexClient(uri, Unchecked.defaultof<HttpClient>, defaultConnectionLimit)
    
    /// <summary>
    /// Only to be used for unit testing
    /// </summary>
    /// <param name="httpClient"></param>
    new(httpClient : HttpClient) = FlexClient(new Uri("http://localhost/"), httpClient, 1)
    
    member this.RequestProcessor<'U>(work : unit -> Task<HttpResponseMessage>) = 
        async { 
            try 
                let! response = Async.AwaitTask(work())
                return! Async.AwaitTask(response.Content.ReadAsAsync<Response<'U>>(mediaTypeFormatters))
            with :? System.AggregateException as e -> 
                let instance = Activator.CreateInstance<Response<'U>>()
                let exn = e.Flatten()
                instance.Error <- (exn.InnerException.Message |> GenerateOperationMessage)
                if exn.InnerException.InnerException <> null then 
                    instance.Error <- (instance.Error |> Append("Reason", exn.InnerException.InnerException.Message))
                return instance
        }
    
    member this.PostHelper<'T, 'U>(uri : string, body : 'T) = 
        async { return! this.RequestProcessor<'U>(fun () -> client.PostAsync(uri, body, mediaTypeFormatter)) }
    member this.PutHelper<'T, 'U>(uri : string, body : 'T) = 
        async { return! this.RequestProcessor<'U>(fun () -> client.PutAsync(uri, body, mediaTypeFormatter)) }
    member this.GetHelper<'U>(uri : string) = 
        async { return! this.RequestProcessor<'U>(fun () -> client.GetAsync(uri)) }
    member this.DeleteHelper<'U>(uri : string) = 
        async { return! this.RequestProcessor<'U>(fun () -> client.DeleteAsync(uri)) }
    interface IFlexClient with
        // Search related
        member this.Search(searchRequest : SearchQuery) : Task<Response<SearchResults>> = 
            Async.StartAsTask
                (this.PostHelper<SearchQuery, SearchResults>
                     (sprintf "/indices/%s/search" searchRequest.IndexName, searchRequest))
        // Index related
        member this.Connect() : Task<Response<unit>> = Async.StartAsTask(this.GetHelper<unit>("/ping"))
        member this.AddIndex(index : Index) : Task<Response<CreateResponse>> = 
            Async.StartAsTask(this.PostHelper<Index, CreateResponse>("/indices", index))
        member this.UpdateIndex(index : Index) : Task<Response<unit>> = 
            Async.StartAsTask(this.PutHelper<Index, unit>(sprintf "/indices/%s" index.IndexName, index))
        member this.DeleteIndex(indexName : string) : Task<Response<unit>> = 
            Async.StartAsTask(this.DeleteHelper<unit>(sprintf "/indices/%s" indexName))
        member this.GetIndex(indexName : string) : Task<Response<Index>> = 
            Async.StartAsTask(this.GetHelper<Index>(sprintf "/indices/%s" indexName))
        member this.GetAllIndex() : Task<Response<List<Index>>> = 
            Async.StartAsTask(this.GetHelper<List<Index>>("/indices"))
        member this.GetIndexStatus(indexName : string) : Task<Response<IndexStatusResponse>> = 
            Async.StartAsTask(this.GetHelper<IndexStatusResponse>(sprintf "/indices/%s/status" indexName))
        member this.IndexExists(indexName : string) : Task<Response<IndexExistsResponse>> = 
            Async.StartAsTask(this.GetHelper<IndexExistsResponse>(sprintf "/indices/%s/exists" indexName))
        member this.BringIndexOnline(indexName : string) : Task<Response<unit>> = 
            Async.StartAsTask(this.PutHelper<unit, unit>(sprintf "/indices/%s/status/online" indexName, ()))
        member this.SetIndexOffline(indexName : string) : Task<Response<unit>> = 
            Async.StartAsTask(this.PutHelper<unit, unit>(sprintf "/indices/%s/status/offline" indexName, ()))
        
        // Document related
        member this.AddDocument(indexName : string, document : FlexDocument) : Task<Response<CreateResponse>> = 
            document.IndexName <- indexName
            Async.StartAsTask
                (this.PostHelper<FlexDocument, CreateResponse>(sprintf "/indices/%s/documents" indexName, document))
        
        member this.DeleteDocument(indexName : string, id : string) : Task<Response<unit>> = 
            failwith "Not implemented yet"
        member this.GetDocument(indexName : string, id : string) : Task<Response<ResultDocument>> = 
            Async.StartAsTask(this.GetHelper<ResultDocument>(sprintf "/indices/%s/documents/%s" indexName id))
        member this.GetTopDocuments(indexName : string, count : int) : Task<Response<SearchResults>> = 
            Async.StartAsTask(this.GetHelper<SearchResults>(sprintf "/indices/%s/documents?count=%i" indexName count))
        member this.UpdateDocument(indexName : string, document : FlexDocument) : Task<Response<unit>> = 
            document.IndexName <- indexName
            Async.StartAsTask
                (this.PutHelper<FlexDocument, unit>(sprintf "/indices/%s/documents/%s" indexName document.Id, document))
