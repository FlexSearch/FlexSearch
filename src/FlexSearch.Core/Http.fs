// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch.Core
open Microsoft.Owin
open Microsoft.Owin.Hosting
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open Owin
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Net
open System.Threading
open System.Threading.Tasks
open Microsoft.Owin.StaticFiles
open Microsoft.Owin.FileSystems

[<AutoOpenAttribute>]
module Http = 
    // ----------------------------------------------------------------------------
    // Http related types and helpers
    // ----------------------------------------------------------------------------         
    type RequestContext = 
        { ResName : string
          ResId : string option
          SubResName : string option
          SubResId : string option
          OwinContext : IOwinContext }
        static member Create(owinContext, resName, ?resId, ?subResName, ?subResId) = 
            { ResName = resName
              ResId = resId
              SubResName = subResName
              SubResId = subResId
              OwinContext = owinContext }
    
    type NoBody() = 
        inherit DtoBase()
        override __.Validate() = ok()
    
    type ResponseContext<'T> = 
        | SomeResponse of responseBody : Choice<'T, Error> * successCode : HttpStatusCode * failureCode : HttpStatusCode
        | SuccessResponse of responseBody : 'T * successCode : HttpStatusCode
        | FailureResponse of responseBody : Error * failureCode : HttpStatusCode
        | FailureOpMsgResponse of responseBody : OperationMessage * failureCode : HttpStatusCode
        | NoResponse
    
    /// Standard FlexSearch response to all web requests 
    type Response<'T> = 
        { [<NullGuard.AllowNullAttribute>]
          Data : 'T
          [<NullGuard.AllowNullAttribute>]
          Error : OperationMessage }
        
        /// Populate response with data part populated
        static member WithData(data) = 
            { Data = data
              Error = Unchecked.defaultof<_> }
        
        /// Populate response with error part populated
        static member WithError(error) = 
            { Data = Unchecked.defaultof<'T>
              Error = error |> toMessage }
    
    let private Formatters = new ConcurrentDictionary<string, IFormatter>(StringComparer.OrdinalIgnoreCase)
    let protoFormatter = new ProtoBufferFormatter() :> IFormatter
    let jsonFormatter = new NewtonsoftJsonFormatter() :> IFormatter
    
    protoFormatter.SupportedHeaders |> Array.iter (fun x -> Formatters.TryAdd(x, protoFormatter) |> ignore)
    jsonFormatter.SupportedHeaders |> Array.iter (fun x -> Formatters.TryAdd(x, jsonFormatter) |> ignore)
    
    /// Get request format from the request object
    /// Defaults to JSON
    let private getRequestFormat (request : IOwinRequest) = 
        if String.IsNullOrWhiteSpace(request.ContentType) then "application/json"
        else request.ContentType
    
    /// Get response format from the OWIN context
    /// Defaults to JSON
    let private getResponseFormat (owin : IOwinContext) = 
        match owin.Request.Accept with
        | null | "*/*" -> 
            match owin.Request.Query.Get("callback") with
            | null -> "application/json"
            | _ -> "application/javascript"
        | x when x.Contains(",") -> owin.Request.Accept.Substring(0, owin.Request.Accept.IndexOf(","))
        | _ -> owin.Request.Accept
    
    /// Write HTTP response
    let writeResponse (statusCode : System.Net.HttpStatusCode) (response : obj) (owin : IOwinContext) = 
        owin.Response.StatusCode <- int statusCode
        let format = getResponseFormat owin
        owin.Response.ContentType <- format
        if response <> Unchecked.defaultof<_> then 
            match Formatters.TryGetValue(format) with
            | true, formatter -> 
                match format with
                // Handle the special jsonp case
                | "application/javascript" -> 
                    use streamWriter = new StreamWriter(owin.Response.Body)
                    streamWriter.Write(owin.Request.Query.Get("callback"))
                    streamWriter.Write("(")
                    streamWriter.Flush()
                    jsonFormatter.Serialize(response, streamWriter.BaseStream)
                    streamWriter.Write(");")
                | _ -> formatter.Serialize(response, owin.Response.Body)
                // *Try* flushing the stream as opposed to always doing it because
                // the stream might have already been closed by the serializer.
                try owin.Response.Body.Flush() with _ -> ()
            | _ -> owin.Response.StatusCode <- int HttpStatusCode.InternalServerError
    
    /// Write HTTP response
    let getRequestBody<'T> (request : IOwinRequest) = 
        let contentType = getRequestFormat request
        if request.Body.CanRead then 
            match Formatters.TryGetValue(contentType) with
            | true, formatter -> 
                try 
                    let result = formatter.DeSerialize<'T>(request.Body)
                    if obj.ReferenceEquals(result, Unchecked.defaultof<_>) then fail HttpNoBodyDefined
                    else ok result
                with ex -> fail <| HttpUnableToParse ex.Message
            | _ -> fail HttpUnsupportedContentType
        else fail HttpNoBodyDefined
    
    let Created = HttpStatusCode.Created
    let Accepted = HttpStatusCode.Accepted
    let Ok = HttpStatusCode.OK
    let NotFound = HttpStatusCode.NotFound
    let BadRequest = HttpStatusCode.BadRequest
    let Conflict = HttpStatusCode.Conflict
    let BAD_REQUEST (value : obj) (owin : IOwinContext) = writeResponse HttpStatusCode.BadRequest value owin
    let NOT_FOUND (value : obj) (owin : IOwinContext) = writeResponse HttpStatusCode.NotFound value owin
    
    let inline CheckIdPresent(owin : IOwinContext) = 
        if owin.Request.Uri.Segments.Length >= 4 then Some(owin.Request.Uri.Segments.[3])
        else None
    
    let inline removeTrailingSlash (input : string) = 
        if input.EndsWith("/") then input.Substring(0, (input.Length - 1))
        else input
    
    let inline getIndexName (owin : IOwinContext) = removeTrailingSlash owin.Request.Uri.Segments.[2]
    let inline subId (owin : IOwinContext) = removeTrailingSlash owin.Request.Uri.Segments.[4]
    
    /// A helper interface to dynamically find all the HttpHandlerBase classes
    type IHttpHandler = 
        abstract Execute : RequestContext -> unit
    
    /// Handler base class which exposes common Http Handler functionality
    [<AbstractClass>]
    type HttpHandlerBase<'T, 'U when 'T :> DtoBase>(?failOnMissingBody : bool, ?validateBody : bool) = 
        member __.HasBody = typeof<'T> <> typeof<NoBody>
        member this.FailOnMissingBody = defaultArg failOnMissingBody this.HasBody
        member __.ValidateBody = defaultArg validateBody false
        member __.DeSerialize(request : IOwinRequest) = getRequestBody<'T> (request)
        
        member __.SerializeSuccess (response : 'U) (successStatus : HttpStatusCode) (owinContext : IOwinContext) = 
            let instance = Response<'U>.WithData(response)
            owinContext |> writeResponse successStatus instance
        
        member __.SerializeFailure (response : Error) (failureStatus : HttpStatusCode) (owinContext : IOwinContext) = 
            let instance = Response<'U>.WithError(response)
            owinContext |> writeResponse failureStatus instance
        
        member this.Serialize (response : Choice<'U, Error>) (successStatus : HttpStatusCode) 
               (failureStatus : HttpStatusCode) (owinContext : IOwinContext) = 
            match response with
            | Choice1Of2(r) -> owinContext |> this.SerializeSuccess r successStatus
            | Choice2Of2(r) -> owinContext |> this.SerializeFailure r failureStatus
        
        abstract Process : request:RequestContext * body:'T option -> ResponseContext<'U>
        interface IHttpHandler with
            member handler.Execute(request : RequestContext) : unit = 
                let validateRequest() = 
                    maybe { 
                        let! body = match handler.HasBody with
                                    | true -> 
                                        match handler.DeSerialize(request.OwinContext.Request) with
                                        | Choice1Of2 a -> ok <| Some(a)
                                        | Choice2Of2 b -> 
                                            if handler.FailOnMissingBody then fail <| b
                                            else ok <| None
                                    | false -> ok <| None
                        /// Validate the DTO
                        if body.IsSome && handler.ValidateBody then do! body.Value.Validate()
                        return body
                    }
                
                let processFailure (error) = 
                    request.OwinContext |> handler.SerializeFailure error HttpStatusCode.BadRequest
                
                let processHandler (body) = 
                    match handler.Process(request, body) with
                    | SomeResponse(body, successCode, failureCode) -> 
                        request.OwinContext |> handler.Serialize body successCode failureCode
                    | SuccessResponse(body, successCode) -> 
                        request.OwinContext |> handler.SerializeSuccess body successCode
                    | FailureResponse(body, failureCode) -> 
                        request.OwinContext |> handler.SerializeFailure body failureCode
                    | FailureOpMsgResponse(opMsg, failureCode) ->
                        request.OwinContext |> writeResponse failureCode { Data = Unchecked.defaultof<'U>; Error = opMsg }
                    | NoResponse -> ()
                
                match validateRequest() with
                | Choice1Of2 body -> processHandler body
                | Choice2Of2 error -> processFailure error
    
    let generateRoutingTable (modules : Dictionary<string, IHttpHandler>) = 
        let result = new Dictionary<string, IHttpHandler>(StringComparer.OrdinalIgnoreCase)
        for m in modules do
            let valueWithSlash, valueWithoutSlash = 
                let intermediate = m.Key.Substring(m.Key.IndexOf("-") + 1)
                let valueWithoutSlash = removeTrailingSlash intermediate
                let valueWithSlash = if intermediate = "/" then "/" else intermediate + "/"
                (valueWithSlash, valueWithoutSlash)
            
            let verb = m.Key.Substring(0, m.Key.IndexOf("-"))
            // check if the key supports more than one http verb by splitting at |
            let verbs = verb.Split([| '|' |], StringSplitOptions.RemoveEmptyEntries)
            for v in verbs do
                result.Add(v + valueWithSlash, m.Value)
                result.Add(v + valueWithoutSlash, m.Value)
        result
    
    type IOwinContext with
        member this.Segment(segNo : int) = this.Request.Uri.Segments.[segNo]
        member this.SegmentWithOutSlash(segNo : int) = removeTrailingSlash this.Request.Uri.Segments.[segNo]
        member this.HttpMethod = this.Request.Method
        member this.RequestContext = 
            match this.Request.Uri.Segments.Length with
            | 1 -> RequestContext.Create(this, "/")
            | 2 -> RequestContext.Create(this, this.Segment(1))
            | 3 -> RequestContext.Create(this, this.SegmentWithOutSlash(1), this.Segment(2))
            | 4 -> RequestContext.Create(this, this.Segment(1), this.SegmentWithOutSlash(2), this.Segment(3))
            | 5 -> RequestContext.Create(this, this.Segment(1), this.SegmentWithOutSlash(2), this.Segment(3), this.Segment(4))
            | _ -> failwithf "Internal Error: FlexSearch does not support URI more than 5 segments."

type IServer = 
    abstract Start : unit -> unit
    abstract Stop : unit -> unit

/// Owin katana server
[<Sealed>]
type OwinServer(httpModule : Dictionary<string, IHttpHandler>, logger : ILogService, ?port0 : int) = 
    let port = defaultArg port0 9800
    let _httpModule = httpModule
    let accessDenied = """
Port access issue. Make sure that the running user has necessary permission to open the port. 
Use the below command to add URL reservation.
---------------------------------------------------------------------------
netsh http add urlacl url=http://+:{port}/ user=everyone listen=yes
---------------------------------------------------------------------------
"""
    
    /// Default OWIN method to process request
    let exec (owin : IOwinContext) = 
        async { 
            let findHandler lookupValue = 
                match _httpModule.TryGetValue(lookupValue) with
                | (true, x) -> Some x
                | _ -> None
            try 
                let httpHandler = 
                    match owin.Request.Uri.Segments.Length with
                    | 1 -> findHandler (owin.HttpMethod + "/") // Server root
                    | 2 -> findHandler (owin.HttpMethod + "/" + owin.Segment(1)) // /Resource
                    | 3 -> findHandler (owin.HttpMethod + "/" + owin.Segment(1) + ":id") // /Resource/:Id
                    | 4 -> findHandler (owin.HttpMethod + "/" + owin.Segment(1) + ":id/" + owin.Segment(3)) // /Resource/:Id/command
                    | 5 -> findHandler (owin.HttpMethod + "/" + owin.Segment(1) + ":id/" + owin.Segment(3) + ":id") // /Resource/:Id/SubResouce/:Id
                    | _ -> None
                match httpHandler with
                | Some(handler) -> handler.Execute(owin.RequestContext)
                | None -> owin |> BAD_REQUEST(Response<unit>.WithError(HttpNotSupported))
            with __ -> ()
        }
    
    /// Default OWIN handler to transform C# function to F#
    let handler = Func<IOwinContext, Func<Task>, Tasks.Task>(fun owin _ -> Async.StartAsTask(exec (owin)) :> Task)
    
    let mutable server = Unchecked.defaultof<IDisposable>
    let mutable thread = Unchecked.defaultof<_>
    member __.Configuration(app : IAppBuilder) = 
        let fileServerOptions = new FileServerOptions()
        fileServerOptions.EnableDirectoryBrowsing <- true
        fileServerOptions.EnableDefaultFiles <- true
        fileServerOptions.FileSystem <- new PhysicalFileSystem(Constants.WebFolder)
        fileServerOptions.RequestPath <- new PathString(@"/static")
        app.UseFileServer(fileServerOptions) |>  ignore
        
        // This should always be the last middleware in the pipeline as this is
        // resposible for handling our REST requests
        app.Use(handler) |> ignore

    interface IServer with
        
        member this.Start() = 
            let startServer() = 
                try 
                    //netsh http add urlacl url=http://+:9800/ user=everyone listen=yes
                    let startOptions = new StartOptions(sprintf "http://+:%i/" port)
                    server <- Microsoft.Owin.Hosting.WebApp.Start(startOptions, this.Configuration)
                with e -> 
                    if e.InnerException <> null then 
                        let innerException = e.InnerException :?> System.Net.HttpListenerException
                        if innerException.ErrorCode = 5 then 
                            // Access denied error
                            e |> Log.fatalWithMsg accessDenied
                        else Log.fatalEx e
                    else Log.fatalEx e
            try 
                thread <- Task.Factory.StartNew(startServer, TaskCreationOptions.LongRunning)
            with e -> Log.fatalEx e
        
        member __.Stop() = server.Dispose()
