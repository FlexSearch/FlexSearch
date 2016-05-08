[<AutoOpen>]
module Helpers

open Fixie
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Kernel
open System
open System.Collections.Generic
open System.Linq
open System.IO
open System.Reflection
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Swensen.Unquote
open FlexSearch.Api
open FlexSearch.Api.Api
open FlexSearch.Api.Client
open FlexSearch.Api.Model
open FSharpx.Task
open Newtonsoft.Json
open FlexSearch.Core.Helpers

module Global =
    open Microsoft.AspNetCore.TestHost

    let mutable RequestLogPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +/ "../../../../documentation/docs/data"
    let server = 
        let serverBuilder = 
            let settings = FlexSearch.Core.Settings.T.GetDefault()
            new FlexSearch.Server.WebServerBuilder(settings)
        new TestServer(serverBuilder.WebAppBuilder)

/// <summary>
/// Represents the lookup name for the plug-in
/// </summary>
[<Sealed>]
[<System.AttributeUsage(System.AttributeTargets.Method)>]
type ExampleAttribute(fileName : string, title : string) = 
    inherit Attribute()
    member this.FileName = fileName
    member this.Title = title

module ResponseLogging =
    let task = new TaskBuilder()

    type ResultLog() = 
        member val Result = Unchecked.defaultof<SearchResults> with get, set
        member val Query = Unchecked.defaultof<SearchQuery> with get, set
        member val Description = Unchecked.defaultof<string> with get, set

    type RequestDetails() = 
        member val RequestNumber = 1 with get, set
        member val HttpRequest = Unchecked.defaultof<HttpRequestMessage> with get, set
        member val RequestBody = Unchecked.defaultof<obj> with get, set
        member val HttpResponse = Unchecked.defaultof<HttpResponseMessage> with get, set
        member val ResponseBody = Unchecked.defaultof<obj> with get, set

    /// <summary>
    /// Simple request logging handler. This is not thread safe. Only use
    /// for testing
    /// </summary>
    type LoggingHandler(innerHandler : HttpMessageHandler) = 
        inherit DelegatingHandler(innerHandler)
        
        let markdownLogs = new List<string>()
        let requestDetailsLogs = new List<RequestDetails>()

        member this.MarkdownLogs() = markdownLogs
        member this.RequestDetailsLogs() = requestDetailsLogs
        member val RequestNumber = 1 with get, set

        override this.SendAsync(request : HttpRequestMessage, cancellationToken) = 
            let log (work : Task<HttpResponseMessage>) = 
                task {
                    let msg = new StringBuilder()
                    let requestLog = new RequestDetails()

                    msg.AppendLine("```html") |> ignore
                    requestLog.HttpRequest <- request
                    msg.AppendLine(request.Method.ToString() + " " + request.RequestUri.ToString() + " HTTP " + request.Version.ToString())
                       .AppendLine("Content-Type: application/json; charset=utf-8")
                       |> ignore

                    // Add body here
                    if not <| isNull request.Content then 
                        let! requestBody = request.Content.ReadAsStringAsync()
                        msg.AppendLine("Content-Length: " + requestBody.Length.ToString())
                           .AppendLine("") |> ignore
                        requestLog.RequestBody <- JsonConvert.DeserializeObject(requestBody)
                        let parsedJson = JsonConvert.DeserializeObject(requestBody)
                        msg.AppendLine(JsonConvert.SerializeObject(parsedJson, Formatting.Indented)) |> ignore
                    else
                        msg.AppendLine("Content-Length: 0") |> ignore
                    msg.AppendLine("")    
                       .AppendLine("-----------------------------") |> ignore
                    let! response = work
                    let! responseBody = response.Content.ReadAsStringAsync()
                    requestLog.HttpResponse <- response
                    requestLog.ResponseBody <- JsonConvert.DeserializeObject(responseBody)
                    let parsedJson = JsonConvert.DeserializeObject(responseBody)
                    msg.AppendLine(JsonConvert.SerializeObject(parsedJson, Formatting.Indented)) |> ignore
                    msg.AppendLine("```") |> ignore
                    requestLog.RequestNumber <- this.RequestNumber

                    msg.ToString() |> markdownLogs.Add
                    requestLog |> requestDetailsLogs.Add
                    this.RequestNumber <- this.RequestNumber + 1

                    return response
                }

            let work = base.SendAsync(request, cancellationToken)
            log (work)

module TestCommandHelpers =
    open ResponseLogging
    open FlexSearch.Core.Helpers
    open System.Net
    open FlexSearch.Api
    let isNotNullOrEmpty (str : string) = String.IsNullOrEmpty(str) |> not
    let hasStatusCode (statusCode : HttpStatusCode) (r : ApiResponse<'T>) = r.StatusCode =? int statusCode; r
    let hasErrorCode (errorCode : string) (r : 'T when 'T :> IResponseError) = r.Error.OperationCode =? errorCode; r
    let hasApiErrorCode (errorCode : string) (r : ApiResponse<'T> when 'T :> IResponseError) = 
        r.Data 
        |> hasErrorCode errorCode 
        |> ignore
        r
    let isSuccessful (r : IResponseError) = 
        if r.Error |> isNull then () 
        else r.Error.Message =? null
    let isCreated (r : ApiResponse<'T> when 'T :> IResponseError) = r.StatusCode =? int HttpStatusCode.Created

    let newIndex indexName = new Index(IndexName = indexName)
    let formatter = new FlexSearch.Core.NewtonsoftJsonFormatter() :> FlexSearch.Core.IFormatter


    /// Write the request details to the specified folder
    /// Force the JIT to not inline this method otherwise Stack frame will return the wrong method name
    [<System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>]
    let log (id : string) (requestNumber : int) (client : LoggingHandler) = 
        if Global.RequestLogPath <> String.Empty && Directory.Exists(Global.RequestLogPath) then 
            let frame = new System.Diagnostics.StackFrame(1)
            let desc = frame.GetMethod().Name
            File.WriteAllText(Global.RequestLogPath +/ id + ".http", client.MarkdownLogs().[requestNumber].ToString())
            File.WriteAllText(Global.RequestLogPath +/ id + ".json", JsonConvert.SerializeObject(client.RequestDetailsLogs().[requestNumber]))
    
    let countryList = JsonConvert.DeserializeObject<List<FlexSearch.Core.Country>>(Resources.DemoIndexData) |> Seq.toList
    
    type SearchCondition = 
        | Predicate of predicate : (FlexSearch.Core.Country -> bool)
        | Expected of expected : int
    
    let inline queryTest (queryString : string) (cond : SearchCondition) (desc : string) (api : SearchApi) = 
        let expected = 
            match cond with
            | Predicate predicate -> 
                countryList
                |> List.where predicate
                |> List.length
            | Expected e -> e
        
        let searchQuery = new SearchQuery("country", queryString)
        searchQuery.Count <- 200
        searchQuery.Columns <- [| "countryname"; "agriproducts"; "governmenttype"; "population" |]
        let response = api.Search("country", searchQuery)
        response |> isSuccessful
        response.Data.TotalAvailable =? expected
        /// Log the result if log path is defined
        if Global.RequestLogPath <> String.Empty && Directory.Exists(Global.RequestLogPath) then 
            let fileName = "search-" + MethodBase.GetCurrentMethod().Name.ToLowerInvariant()
            
            let result = new ResultLog()
            result.Query <- searchQuery
            result.Result <- response.Data
            result.Description <- desc
            File.WriteAllText
                (Global.RequestLogPath +/ fileName + ".json", JsonConvert.SerializeObject(result, Formatting.Indented))
    

[<AutoOpen>]
module FixtureSetup =
    open ResponseLogging
    open TestCommandHelpers
    open Newtonsoft.Json

    /// Basic test index with all field types
    let getTestIndex() = 
        let index = new Index(IndexName = Guid.NewGuid().ToString("N"))
        index.IndexConfiguration <- new IndexConfiguration(CommitOnClose = false, AutoCommit = false, AutoRefresh = false)
        index.Active <- true
        index.IndexConfiguration.DirectoryType <- Constants.DirectoryType.MemoryMapped
        index.Fields <- [| new Field("name", Constants.FieldType.Text) |]
        index

    let httpMessageHandler = Global.server.CreateHandler()
    let catchAllLoggingHandler = new LoggingHandler(httpMessageHandler)
    let flexClient = new ApiClient(catchAllLoggingHandler)

    let mockIndexSettings() = 
        let index = new Index()
        index.IndexName <- "contact"
        index.IndexConfiguration <- new IndexConfiguration(CommitOnClose = false, AutoCommit = false, AutoRefresh = false)
        index.Active <- true
        index.IndexConfiguration.DirectoryType <- Constants.DirectoryType.Ram
        index.Fields <- 
         [| new Field("firstname", Constants.FieldType.Text)
            new Field("lastname", Constants.FieldType.Text)
            new Field("email", Constants.FieldType.Keyword)
            new Field("country", Constants.FieldType.Text)
            new Field("ipaddress", Constants.FieldType.Keyword)
            new Field("cvv2", Constants.FieldType.Int)
            new Field("description", Constants.FieldType.Text)
            new Field("fullname", Constants.FieldType.Text) |]
        
        let api = new IndicesApi(flexClient)
        if not <| api.IndexExists("contact").Data.Exists
        then api.CreateIndex(index) |> isSuccessful
        index
    
    let apiGenerator (ctor : ApiClient -> 'T) = 
        let handler = new LoggingHandler(httpMessageHandler)
        let api = new ApiClient(handler)
        (ctor(api), handler)

    let fixtureCustomization () =
        let fixture = new Ploeh.AutoFixture.Fixture()
        // We override Auto fixture's string generation mechanism to return this string which will be
        // used as index name
        fixture.Register<String>(fun _ -> Guid.NewGuid().ToString("N"))
        fixture.Register<Index>(fun _ -> getTestIndex()) |> ignore
        fixture.Inject<ApiClient>(flexClient)
        fixture.Inject<IndicesApi>(new IndicesApi(flexClient))
        fixture.Inject<CommonApi>(new CommonApi(flexClient))
        fixture.Inject<DocumentsApi>(new DocumentsApi(flexClient))
        fixture.Inject<AnalyzerApi>(new AnalyzerApi(flexClient))
        fixture.Inject<JobsApi>(new JobsApi(flexClient))
        fixture.Inject<SearchApi>(new SearchApi(flexClient))
        fixture.Inject<ServerApi>(new ServerApi(flexClient))
        fixture.Register<ServerApi * LoggingHandler>(fun _ -> apiGenerator ServerApi)
        fixture.Register<IndicesApi * LoggingHandler>(fun _ -> apiGenerator IndicesApi)
        fixture.Register<CommonApi * LoggingHandler>(fun _ -> apiGenerator CommonApi)
        fixture.Register<DocumentsApi * LoggingHandler>(fun _ -> apiGenerator DocumentsApi)
        fixture.Register<AnalyzerApi * LoggingHandler>(fun _ -> apiGenerator AnalyzerApi)
        fixture.Register<JobsApi * LoggingHandler>(fun _ -> apiGenerator JobsApi)
        fixture.Register<SearchApi * LoggingHandler>(fun _ -> apiGenerator SearchApi)
        fixture.Inject<FlexSearch.Core.Country list>(countryList)
        fixture

// ----------------------------------------------------------------------------
// Convention Section for Fixie
// ----------------------------------------------------------------------------
/// Custom attribute to create parameterised test
[<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
type InlineDataAttribute([<System.ParamArrayAttribute>] parameters : obj []) = 
    inherit Attribute()
    member val Parameters = parameters

type InputParameterSource() = 
    interface ParameterSource with
        member __.GetParameters(methodInfo : MethodInfo) = 
            // Check if the method contains inline data attribute. If not then use AutoFixture
            // to generate input value
            let customAttribute = methodInfo.GetCustomAttributes<InlineDataAttribute>(true)
            if customAttribute.Any() then customAttribute.Select(fun input -> input.Parameters)
            else 
                let fixture = fixtureCustomization()
                let create (builder : ISpecimenBuilder, typ : Type) = (new SpecimenContext(builder)).Resolve(typ)
                let parameterTypes = methodInfo.GetParameters().Select(fun x -> x.ParameterType)
                let parameterValues = parameterTypes.Select(fun x -> create (fixture, x)).ToArray()
                seq { yield parameterValues }

type SingleInstancePerClassConvention() as self = 
    inherit Convention()
    
    let fixtureFactory (typ : Type) = 
        let fixture = fixtureCustomization()
        (new SpecimenContext(fixture)).Resolve(typ)
    
    do
        printfn "RequestLogPath: %A" self.Options.["requestlogpath"]
        if self.Options.["requestlogpath"].Count = 1 then
            Global.RequestLogPath <- self.Options.["requestlogpath"].[0]

        if isNotBlank Global.RequestLogPath then createDir Global.RequestLogPath

        self.Classes.NameEndsWith([| "Tests"; "Test"; "test"; "tests" |]) |> ignore
        // Temporarily ignore parametric tests because Fixie doesn't handle them in VS 2015
        // Comment out this line if you want to also execute ignored tests
        //self.Methods.Where(fun m -> m.HasOrInherits<IgnoreAttribute>() |> not) |> ignore
        self.ClassExecution.CreateInstancePerClass().UsingFactory(fun typ -> fixtureFactory (typ)) |> ignore
        self.Parameters.Add<InputParameterSource>() |> ignore