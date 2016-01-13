﻿[<AutoOpen>]
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


module Global =
    open Microsoft.AspNet.TestHost

    let mutable RequestLogPath = String.Empty
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
        member val HttpRequest = Unchecked.defaultof<HttpRequestMessage> with get, set
        member val RequestBody = Unchecked.defaultof<string> with get, set
        member val HttpResponse = Unchecked.defaultof<HttpResponseMessage> with get, set
        member val ResponseBody = Unchecked.defaultof<string> with get, set

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

module TestCommandHelpers =
    open ResponseLogging
    open FlexSearch.Core.Helpers
    open System.Net

    let newIndex indexName = new Index(IndexName = indexName)
    let formatter = new FlexSearch.Core.NewtonsoftJsonFormatter() :> FlexSearch.Core.IFormatter

    /// Write the request details to the specified folder
    /// Force the JIT to not inline this method otherwise Stack frame will return the wrong method name
    [<System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>]
    let log (id : string) (client : LoggingHandler) = 
        if Global.RequestLogPath <> String.Empty && Directory.Exists(Global.RequestLogPath) then 
            let frame = new System.Diagnostics.StackFrame(1)
            let desc = frame.GetMethod().Name
            File.WriteAllText(Global.RequestLogPath +/ id + ".http", client.Log().ToString())
    
    /// Force the JIT to not inline this method otherwise Stack frame will return the wrong method name
    [<System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>]
    let query (queryString : string) (recordsReturned : int) (available : int) (api : SearchApi) = 
        let searchQuery = new SearchQuery("country", queryString)
        searchQuery.Count <- 10
        searchQuery.Columns <- [| "countryname"; "agriproducts"; "governmenttype"; "population" |]
        let response = api.PostSearch(searchQuery, "country")
        response.Data.TotalAvailable =? recordsReturned
        /// Log the result if log path is defined
        if Global.RequestLogPath <> String.Empty && Directory.Exists(Global.RequestLogPath) then 
            let frame = new System.Diagnostics.StackFrame(1)
            
            let meth = frame.GetMethod()
            match meth.CustomAttributes |> Seq.tryFind (fun x -> x.AttributeType = typeof<ExampleAttribute>) with
            | Some(attr) -> 
                let fileName = attr.ConstructorArguments.[0].ToString().Replace('"', ' ').Trim()

                let desc = 
                    if not <| String.IsNullOrWhiteSpace(attr.ConstructorArguments.[1].ToString()) then 
                        attr.ConstructorArguments.[1].ToString().Replace('"', ' ').Trim()
                    else meth.Name
                
                let result = new ResultLog()
                result.Query <- searchQuery
                result.Result <- response.Data
                result.Description <- desc
                File.WriteAllText(Global.RequestLogPath +/ fileName + ".json", formatter.SerializeToString(result))
            | None -> ()

    let isNotNullOrEmpty (str : string) = String.IsNullOrEmpty(str) |> not

    let hasStatusCode (statusCode : HttpStatusCode) (r : ApiResponse<'T>) = r.StatusCode =? int statusCode; r
    let hasErrorCode (errorCode : string) (r : 'T when 'T :> Response) = r.Error.ErrorCode =? errorCode; r
    let hasApiErrorCode (errorCode : string) (r : ApiResponse<'T> when 'T :> Response) = 
        r.Data 
        |> hasErrorCode errorCode 
        |> ignore
        r
    let isSuccessful (r : Response) = r.Error =? null
    let isCreated (r : ApiResponse<'T> when 'T :> Response) = r.StatusCode =? int HttpStatusCode.Created

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
        index.Fields <- [| new Field("b1", Constants.FieldType.Bool)
                           new Field("b2", Constants.FieldType.Bool)
                           new Field("d1", Constants.FieldType.Date)
                           new Field("dt1", Constants.FieldType.DateTime)
                           new Field("db1", Constants.FieldType.Double)
                           new Field("et1", Constants.FieldType.ExactText, AllowSort = true)
                           new Field("h1", Constants.FieldType.Highlight)
                           new Field("i1", Constants.FieldType.Int)
                           new Field("i2", Constants.FieldType.Int, AllowSort = true)
                           new Field("l1", Constants.FieldType.Long)
                           new Field("t1", Constants.FieldType.Text)
                           new Field("t2", Constants.FieldType.Text)
                           new Field("s1", Constants.FieldType.Stored) |]
        index

    let httpMessageHandler = Global.server.CreateHandler()
    let flexClient = new ApiClient(httpMessageHandler)

    let mockIndexSettings = 
        let index = new Index()
        index.IndexName <- "contact"
        index.IndexConfiguration <- new IndexConfiguration(CommitOnClose = false, AutoCommit = false, AutoRefresh = false)
        index.Active <- true
        index.IndexConfiguration.DirectoryType <- Constants.DirectoryType.Ram
        index.Fields <- 
         [| new Field("firstname", Constants.FieldType.Text)
            new Field("lastname", Constants.FieldType.Text)
            new Field("email", Constants.FieldType.ExactText)
            new Field("country", Constants.FieldType.Text)
            new Field("ipaddress", Constants.FieldType.ExactText)
            new Field("cvv2", Constants.FieldType.Int)
            new Field("description", Constants.FieldType.Highlight)
            new Field("fullname", Constants.FieldType.Text) |]
        
        let api = new IndicesApi(flexClient)
        if not <| api.IndexExists("contact").Data.Exists
        then api.CreateIndex(index) |> isSuccessful
        index

    let countryList = JsonConvert.DeserializeObject<List<FlexSearch.Core.Country>>(Resources.DemoIndexData)

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
        fixture.Inject<LoggingHandler>(new LoggingHandler(httpMessageHandler))
        fixture.Inject<FlexSearch.Core.Country list>(countryList |> Seq.toList)
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

        self.Classes.NameEndsWith([| "Tests"; "Test"; "test"; "tests" |]) |> ignore
        // Temporarily ignore parametric tests because Fixie doesn't handle them in VS 2015
        // Comment out this line if you want to also execute ignored tests
        //self.Methods.Where(fun m -> m.HasOrInherits<IgnoreAttribute>() |> not) |> ignore
        self.ClassExecution.CreateInstancePerClass().UsingFactory(fun typ -> fixtureFactory (typ)) |> ignore
        self.Parameters.Add<InputParameterSource>() |> ignore