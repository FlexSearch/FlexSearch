module Client

open System
open System.Net
open System.Net.Http
open System.Net.Http.Formatting
open System.Threading.Tasks
open FlexSearch.Core
open System.Text
open Newtonsoft.Json
open System.Net.Http.Headers
open System.IO
open Newtonsoft.Json.Bson
open System.Linq
open System.Collections
open FlexSearch.Api.Models

[<AutoOpen>]
module Helper =
    open FSharpx.Task

    let task = new TaskBuilder()

type RequestDetails() = 
    member val HttpRequest = Unchecked.defaultof<HttpRequestMessage> with get, set
    member val RequestBody = Unchecked.defaultof<string> with get, set
    member val HttpResponse = Unchecked.defaultof<HttpResponseMessage> with get, set
    member val ResponseBody = Unchecked.defaultof<string> with get, set

type JsonNetMediaTypeFormatter(serializerSettings : JsonSerializerSettings) as this =
    inherit MediaTypeFormatter()
    
    do
        if serializerSettings = null then raise (new ArgumentException("serializerSettings"))
        this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"))
        this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"))
        this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/bson"))

    override __.WriteToStreamAsync(typ : Type, value : obj, stream : Stream, content : HttpContent, context : TransportContext) =
        Task.Factory.StartNew(fun () ->
            let serializer = JsonSerializer.Create(serializerSettings)

            let getWriter (contentHeaders : HttpContentHeaders) (stream : Stream) =
                if contentHeaders.ContentType.MediaType.EndsWith("json")
                then new JsonTextWriter(new StreamWriter(stream)) :> JsonWriter
                else new BsonWriter(stream) :> JsonWriter

            // NOTE: we don't dispose or close the writer as that would 
            // close the stream, which is used by the rest of the pipeline.
            let writer = stream |> getWriter content.Headers

            if typ.IsGenericType && typ.GetGenericTypeDefinition() = typeof<IQueryable<_>>
            then serializer.Serialize(writer, (value :?> IEnumerable).OfType<obj>().ToList())
            else serializer.Serialize(writer, value)

            writer.Flush() 
        )

    override __.ReadFromStreamAsync(typ : Type, stream : Stream, content : HttpContent, logger : IFormatterLogger) =
        task {
            let serializer = JsonSerializer.Create(serializerSettings)

            let getReader (contentHeaders : HttpContentHeaders) (stream : Stream) =
                if contentHeaders.ContentType.MediaType.EndsWith("json")
                then new JsonTextReader(new StreamReader(stream)) :> JsonReader
                else new BsonReader(stream) :> JsonReader

            let reader = stream |> getReader content.Headers

            return serializer.Deserialize(reader, typ)
        }

    override __.CanReadType (t : Type) = true
    override __.CanWriteType (t : Type) = true

    new () = 
        let settings = new JsonSerializerSettings(ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
        JsonNetMediaTypeFormatter(settings)


/// <summary>
/// Simple request logging handler. This is not thread safe. Only use
/// for testing
/// </summary>
type LoggingHandler(innerHandler : HttpMessageHandler) = 
    inherit DelegatingHandler(innerHandler)
    let log = new StringBuilder()
    let requestLog = new RequestDetails()
    let mutable statusCode = System.Net.HttpStatusCode.OK
    member this.Log() = log
    member this.RequestLog() = requestLog
    member this.StatusCode() = statusCode
    override this.SendAsync(request : HttpRequestMessage, cancellationToken) = 
        let log (work : Task<HttpResponseMessage>) = 
            task { 
                requestLog.HttpRequest <- request
                log.AppendLine("Request") |> ignore
                log.Append(request.Method.ToString() + " ") |> ignore
                log.AppendLine(request.RequestUri.ToString()) |> ignore
                // Add body here
                if request.Content <> null then 
                    let! requestBody = request.Content.ReadAsStringAsync()
                    requestLog.RequestBody <- requestBody
                    log.AppendLine(requestBody) |> ignore
                log.AppendLine("----------------------------------") |> ignore
                log.AppendLine("Response") |> ignore
                let! response = work
                statusCode <- response.StatusCode
                let! responseBody = response.Content.ReadAsStringAsync()
                requestLog.HttpResponse <- response
                requestLog.ResponseBody <- responseBody
                log.AppendLine(responseBody) |> ignore
                return response
            }
        
        let work = base.SendAsync(request, cancellationToken)
        log (work)

open Newtonsoft.Json.Serialization

type FlexClient(uri : Uri, httpClient : HttpClient, ?defaultConnectionLimit : int) = 
    let mutable client = httpClient
    let connectionLimit = defaultArg defaultConnectionLimit 200
    let mediaTypeFormatter = new JsonNetMediaTypeFormatter()
    let mediaTypeFormatters = [ new JsonNetMediaTypeFormatter() :> MediaTypeFormatter ]
    let GenerateOperationMessage(input : string) = 
        assert (input.Contains(":"))
        { Properties = Array.empty
          Message = input.Substring(input.IndexOf(":") + 1)
          ErrorCode = input.Substring(0, input.IndexOf(":")) }
    let Append (key, value) (message : OperationMessage) = 
        { message with Message = sprintf "%s; %s = '%s'" message.Message key value }
    let getName (typ : Type) =
        let att = Attribute.GetCustomAttribute(typ, typeof<NameAttribute>) :?> NameAttribute
        att.Name

    do 
        if client = Unchecked.defaultof<_> then 
            client <- new HttpClient()
            client.BaseAddress <- uri
        else client.BaseAddress <- uri
        System.Net.ServicePointManager.DefaultConnectionLimit <- connectionLimit
    
    new(uri : Uri) = FlexClient(uri, Unchecked.defaultof<HttpClient>, 200)
    new(uri : Uri, defaultConnectionLimit : int) = 
        FlexClient(uri, Unchecked.defaultof<HttpClient>, defaultConnectionLimit)
    
    /// <summary>
    /// Only to be used for unit testing
    /// </summary>
    /// <param name="httpClient"></param>
    new(httpClient : HttpClient) = FlexClient(new Uri("http://localhost/"), httpClient, 1)
    
    member this.RequestProcessor<'U>(work : unit -> Task<HttpResponseMessage>) = 
        task {
            let! response = work()
            let! obj = response.Content.ReadAsAsync<Response<'U>>(mediaTypeFormatters)
            return (obj, response.StatusCode)
        }

    member this.PostHelper<'T, 'U>(uri : string, body : 'T) = 
        task { return! this.RequestProcessor<'U>(fun () -> client.PostAsync(uri, body, mediaTypeFormatter)) }
    member this.PutHelper<'T, 'U>(uri : string, body : 'T) = 
        task { return! this.RequestProcessor<'U>(fun () -> client.PutAsync(uri, body, mediaTypeFormatter)) }
    member this.GetHelper<'U>(uri : string) = 
        task { return! this.RequestProcessor<'U>(fun () -> client.GetAsync(uri)) }
    member this.DeleteHelper<'U>(uri : string) = 
        task { return! this.RequestProcessor<'U>(fun () -> client.DeleteAsync(uri)) }


    // --------------------
    // Search related
    // --------------------
    member this.Search(searchRequest : SearchQuery) = 
        this.PostHelper<SearchQuery, SearchResults>
            (sprintf "/indices/%s/search" searchRequest.IndexName, searchRequest)
    // --------------------
    // Index related
    // --------------------
    member this.Connect() = this.GetHelper<unit>("/ping")
    member this.AddIndex(index : Index) = 
        this.PostHelper<Index, CreateResponse>("/indices", index)
    member this.UpdateIndex(index : Index) = 
        this.PutHelper<Index, unit>(sprintf "/indices/%s" index.IndexName, index)
//    member this.UpdateIndexFields(indexName : string, fields : FieldsUpdateRequest) = 
//        this.PutHelper<FieldsUpdateRequest, unit>(sprintf "/indices/%s/fields" indexName, fields)
    member this.UpdateIndexSearchProfile(indexName : string, profile : SearchQuery) = 
        this.PutHelper<SearchQuery, unit>(sprintf "/indices/%s/searchprofile" indexName, profile)
    member this.UpdateIndexConfiguration(indexName : string, conf : IndexConfiguration) = 
        this.PutHelper<IndexConfiguration, unit>(sprintf "/indices/%s/configuration" indexName, conf)
    member this.DeleteIndex(indexName : string) = 
        this.DeleteHelper<unit>(sprintf "/indices/%s" indexName)
    member this.GetIndex(indexName : string) = 
        this.GetHelper<Index>(sprintf "/indices/%s" indexName)
    member this.GetAllIndex()  = 
        this.GetHelper<List<Index>>("/indices")
    member this.GetIndexStatus(indexName : string)  = 
        this.GetHelper<IndexStatusResponse>(sprintf "/indices/%s/status" indexName)
    member this.IndexExists(indexName : string)  = 
        this.GetHelper<IndexExistsResponse>(sprintf "/indices/%s/exists" indexName)
    member this.BringIndexOnline(indexName : string)  = 
        this.PutHelper<unit, unit>(sprintf "/indices/%s/status/online" indexName, ())
    member this.SetIndexOffline(indexName : string)  = 
        this.PutHelper<unit, unit>(sprintf "/indices/%s/status/offline" indexName, ())
        
    // --------------------
    // Document related
    // --------------------
    member this.AddDocument(indexName : string, document : Document) = 
        document.IndexName <- indexName
        this.PostHelper<Document, CreateResponse>(sprintf "/indices/%s/documents" indexName, document)
    member this.DeleteDocument(indexName : string, id : string)  = 
        this.DeleteHelper<unit>(sprintf "/indices/%s/documents/%s" indexName id)
    member this.DeleteAllDocuments(indexName : string)  = 
        this.DeleteHelper<unit>(sprintf "/indices/%s/documents" indexName)
    member this.GetDocument(indexName : string, id : string)  = 
        this.GetHelper<Document>(sprintf "/indices/%s/documents/%s" indexName id)
    member this.GetTopDocuments(indexName : string, count : int)  = 
        this.GetHelper<SearchResults>(sprintf "/indices/%s/documents?count=%i" indexName count)
    member this.UpdateDocument(indexName : string, document : Document) = 
        document.IndexName <- indexName
        this.PutHelper<Document, unit>(sprintf "/indices/%s/documents/%s" indexName document.Id, document)

    // --------------------
    // Demo index related
    // --------------------
    member this.SetupDemo() = this.PutHelper<unit,unit>(sprintf "/setupdemo", ())
