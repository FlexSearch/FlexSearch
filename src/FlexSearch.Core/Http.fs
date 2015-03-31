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
open System.ComponentModel.Composition
open System.IO
open System.Net
open System.Threading
open System.Threading.Tasks

// ----------------------------------------------------------------------------
// Formatter section : All the various media formatter to be used in 
// Flexsearch
// ----------------------------------------------------------------------------
/// Formatter interface for supporting multiple formats in the HTTP engine
type IFormatter = 
    abstract SupportedHeaders : unit -> string []
    abstract Serialize : body:obj * stream:Stream -> unit
    abstract Serialize : body:obj * context:IOwinContext -> unit
    abstract SerializeToString : body:obj -> string
    abstract DeSerialize<'T> : stream:Stream -> 'T

[<Sealed>]
type JilJsonFormatter() = 
    let options = new Jil.Options(false, false, false, Jil.DateTimeFormat.ISO8601, false)
    interface IFormatter with
        member __.SerializeToString(_ : obj) = failwith "Not implemented yet"
        
        member __.DeSerialize<'T>(stream : Stream) = 
            use reader = new StreamReader(stream) :> TextReader
            Jil.JSON.Deserialize<'T>(reader, options)
        
        member __.Serialize(body : obj, stream : Stream) : unit = 
            use writer = new StreamWriter(stream)
            Jil.JSON.Serialize(body, writer, options)
        
        member __.Serialize(body : obj, context : IOwinContext) = 
            use writer = new StreamWriter(context.Response.Body)
            Jil.JSON.Serialize(body, writer, options)
        
        member __.SupportedHeaders() : string [] = 
            [| "application/json"; "text/json"; "application/json;charset=utf-8"; "application/json; charset=utf-8" |]

[<Sealed>]
type NewtonsoftJsonFormatter() = 
    let options = new Newtonsoft.Json.JsonSerializerSettings()
    do options.Converters.Add(new StringEnumConverter())
    interface IFormatter with
        member __.SerializeToString(_ : obj) = failwith "Not implemented yet"
        
        member __.DeSerialize<'T>(stream : Stream) = 
            use reader = new StreamReader(stream)
            JsonConvert.DeserializeObject<'T>(reader.ReadToEnd(), options)
        
        member __.Serialize(body : obj, stream : Stream) : unit = 
            use writer = new StreamWriter(stream)
            let body = JsonConvert.SerializeObject(body, options)
            writer.Write(body)
        
        member __.Serialize(body : obj, context : IOwinContext) = 
            use writer = new StreamWriter(context.Response.Body)
            let body = JsonConvert.SerializeObject(body, options)
            match context.Request.Query.Get("callback") with
            | null -> writer.Write(body)
            | value -> 
                context.Response.ContentType <- "application/javascript"
                writer.Write(value + "(")
                writer.Write(body)
                writer.Write(")")
        
        member __.SupportedHeaders() : string [] = 
            [| "application/json"; "text/json"; "application/json;charset=utf-8"; "application/json; charset=utf-8"; 
               "application/javascript" |]

[<Sealed>]
type ProtoBufferFormatter() = 
    let serialize (body : obj, stream : Stream) = ProtoBuf.Serializer.Serialize(stream, body)
    interface IFormatter with
        member __.SerializeToString(_ : obj) = failwith "Not implemented yet"
        member __.DeSerialize<'T>(stream : Stream) = ProtoBuf.Serializer.Deserialize<'T>(stream)
        member __.Serialize(body : obj, stream : Stream) : unit = serialize (body, stream)
        member __.Serialize(body : obj, context : IOwinContext) : unit = serialize (body, context.Response.Body)
        member __.SupportedHeaders() : string [] = [| "application/x-protobuf"; "application/octet-stream" |]

[<Sealed>]
type YamlFormatter() = 
    let options = YamlDotNet.Serialization.SerializationOptions.EmitDefaults
    let serializer = new YamlDotNet.Serialization.Serializer(options)
    let deserializer = new YamlDotNet.Serialization.Deserializer(ignoreUnmatched = true)
    
    let serialize (body : obj, stream : Stream) = 
        use TextWriter = new StreamWriter(stream)
        serializer.Serialize(TextWriter, body)
    
    interface IFormatter with
        
        member __.SerializeToString(body : obj) = 
            use textWriter = new StringWriter()
            serializer.Serialize(textWriter, body)
            textWriter.ToString()
        
        member __.DeSerialize<'T>(stream : Stream) = 
            use textReader = new StreamReader(stream)
            deserializer.Deserialize<'T>(textReader)
        
        member __.Serialize(body : obj, stream : Stream) : unit = serialize (body, stream)
        member __.Serialize(body : obj, context : IOwinContext) : unit = serialize (body, context.Response.Body)
        member __.SupportedHeaders() : string [] = [| "application/yaml" |]

[<AutoOpenAttribute>]
module Http = 
    // ----------------------------------------------------------------------------
    // Http related types and helpers
    // ----------------------------------------------------------------------------     
    /// Http Handler properties to define the behaviour of
    /// Http Rest web service
    type HttpHandlerProperties = 
        { /// Throw error if the request is missing 
          /// the body
          FailOnMissingBody : bool
          /// Validate if the given index is present
          CheckIndexExists : bool
          /// Check if the given index is online or not?
          CheckIndexIsOnline : bool
          /// Validate the request or not?
          ValidateDto : bool
          /// Set the request DTO values from the query string
          SetValuesFromQueryString : bool }
        
        static member CompleteControl = 
            { FailOnMissingBody = false
              CheckIndexExists = false
              CheckIndexIsOnline = false
              ValidateDto = false
              SetValuesFromQueryString = false }
        
        static member OnlineIndex = 
            { FailOnMissingBody = true
              CheckIndexExists = true
              CheckIndexIsOnline = true
              ValidateDto = true
              SetValuesFromQueryString = true }
        
        static member IndexExists = 
            { FailOnMissingBody = true
              CheckIndexExists = true
              CheckIndexIsOnline = false
              ValidateDto = true
              SetValuesFromQueryString = true }
    
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
    
    type ResponseContext<'T> = 
        | SomeResponse of responseBody : Choice<'T, Error> * successCode : HttpStatusCode * failureCode : HttpStatusCode
        | SuccessResponse of responseBody : 'T * successCode : HttpStatusCode
        | FailureResponse of responseBody : Error * failureCode : HttpStatusCode
        | NoResponse
    
    /// Standard FlexSearch response to all web requests 
    type Response<'T> = 
        { Data : 'T
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
    
    protoFormatter.SupportedHeaders() |> Array.iter (fun x -> Formatters.TryAdd(x, protoFormatter) |> ignore)
    jsonFormatter.SupportedHeaders() |> Array.iter (fun x -> Formatters.TryAdd(x, jsonFormatter) |> ignore)
    
    /// Get request format from the request object
    /// Defaults to JSON
    let private getRequestFormat (request : IOwinRequest) = 
        if String.IsNullOrWhiteSpace(request.ContentType) then "application/json"
        else request.ContentType
    
    /// Get response format from the OWIN context
    /// Defaults to JSON
    let private getResponseFormat (owin : IOwinContext) = 
        if owin.Request.Accept = null then "application/json"
        else if owin.Request.Accept = "*/*" then "application/json"
        else if owin.Request.Accept.Contains(",") then 
            owin.Request.Accept.Substring(0, owin.Request.Accept.IndexOf(","))
        else owin.Request.Accept
    
    /// Write HTTP response
    let writeResponse (statusCode : System.Net.HttpStatusCode) (response : obj) (owin : IOwinContext) = 
        owin.Response.StatusCode <- int statusCode
        let format = getResponseFormat owin
        owin.Response.ContentType <- format
        if response <> Unchecked.defaultof<_> then 
            match Formatters.TryGetValue(format) with
            | true, formatter -> formatter.Serialize(response, owin)
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
    
    type IHttpHandler<'T, 'U when 'T :> IValidate<'T>> = 
        abstract Process : request:RequestContext * body:'T option -> ResponseContext<'U>
        abstract Properties : HttpHandlerProperties
        abstract HasBody : bool
        abstract DeSerialize : IOwinRequest -> Choice<'T, Error>
        abstract SerializeSuccess : 'U -> HttpStatusCode -> IOwinContext -> unit
        abstract SerializeFailure : Error -> HttpStatusCode -> IOwinContext -> unit
        abstract Serialize : Choice<'U, Error> -> HttpStatusCode -> HttpStatusCode -> IOwinContext -> unit
    
    /// Handler base class which exposes common Http Handler functionality
    [<AbstractClass>]
    type HttpHandlerBase<'T, 'U when 'T :> IValidate<'T>>(?properties0 : HttpHandlerProperties) = 
        let properties = defaultArg properties0 HttpHandlerProperties.OnlineIndex
        
        let hasBody = 
            if typeof<'T> = typeof<unit> then false
            else true
        
        let deSerialize (request : IOwinRequest) = getRequestBody<'T> (request)
        
        let serializeSuccess (response : 'U) (successStatus : HttpStatusCode) (owinContext : IOwinContext) = 
            let instance = Response<'U>.WithData(response)
            owinContext |> writeResponse successStatus instance
        
        let serializeFailure (response : Error) (failureStatus : HttpStatusCode) (owinContext : IOwinContext) = 
            let instance = Response<'U>.WithError(response)
            owinContext |> writeResponse failureStatus instance
        
        let serialize (response : Choice<'U, Error>) (successStatus : HttpStatusCode) (failureStatus : HttpStatusCode) 
            (owinContext : IOwinContext) = 
            match response with
            | Choice1Of2(r) -> owinContext |> serializeSuccess r successStatus
            | Choice2Of2(r) -> owinContext |> serializeFailure r failureStatus
        
        abstract Process : request:RequestContext * body:'T option -> ResponseContext<'U>
        interface IHttpHandler<'T, 'U> with
            member __.DeSerialize(request) = deSerialize (request)
            member __.HasBody = hasBody
            member this.Process(request, body) = this.Process(request, body)
            member __.Properties = properties
            member __.SerializeSuccess response successStatus owinContext = 
                owinContext |> serializeSuccess response successStatus
            member __.SerializeFailure error failureStatusCode owinContext = 
                owinContext |> serializeFailure error failureStatusCode
            member __.Serialize result successCode failureCode owinContext = 
                owinContext |> serialize result successCode failureCode
    
    /// This is responsible for generating routing lookup table from the registerd http modules
    let generateRoutingTable (modules : Dictionary<string, HttpHandlerBase<_, _>>) = 
        let result = new Dictionary<string, HttpHandlerBase<_, _>>(StringComparer.OrdinalIgnoreCase)
        for m in modules do
            let valueWithSlash, valueWithoutSlash = 
                let intermediate = m.Key.Substring(m.Key.IndexOf("-") + 1)
                let valueWithoutSlash = removeTrailingSlash intermediate
                let valueWithSlash = intermediate + "/"
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
            | 4 -> RequestContext.Create(this, this.Segment(1), this.Segment(2), this.Segment(3))
            | 5 -> RequestContext.Create(this, this.Segment(1), this.Segment(2), this.Segment(3), this.Segment(4))
            | _ -> failwithf "Internal Error: FlexSearch does not support URI more than 5 segments."

type IServer = 
    abstract Start : unit -> unit
    abstract Stop : unit -> unit

/// Owin katana server
[<Sealed>]
type OwinServer(indexExists : string -> Choice<unit, Error>, indexOnline : string -> Choice<unit, Error>, httpModule : Dictionary<string, IHttpHandler<_, Error>>, logger : ILogService, ?port0 : int) = 
    let port = defaultArg port0 9800
    let accessDenied = """
Port access issue. Make sure that the running user has necessary permission to open the port. 
Use the below command to add URL reservation.
---------------------------------------------------------------------------
netsh http add urlacl url=http://+:{port}/ user=everyone listen=yes
---------------------------------------------------------------------------
"""
    
    let execute (request : RequestContext, handler : IHttpHandler<_, _>) = 
        let validateRequest() = 
            maybe { 
                /// Check if we are in processing indices based resource
                let! checks = if String.Equals(request.ResName, "indices", StringComparison.OrdinalIgnoreCase) 
                                 && request.ResId.IsSome then 
                                  match handler.Properties.CheckIndexIsOnline, handler.Properties.CheckIndexExists with
                                  | true, _ -> indexOnline request.ResId.Value
                                  | false, true -> indexExists request.ResId.Value
                                  | _ -> ok()
                              else ok()
                let! body = match handler.HasBody with
                            | true -> 
                                match handler.DeSerialize(request.OwinContext.Request) with
                                | Choice1Of2 a -> 
                                    // Set the default value for the DTO
                                    ok <| Some(a.SetDefaults())
                                | Choice2Of2 b -> 
                                    if handler.Properties.FailOnMissingBody then fail <| b
                                    else ok <| None
                            | false -> ok <| None
                /// Validate the DTO
                if body.IsSome && handler.Properties.ValidateDto then do! body.Value.Validate()
                return body
            }
        
        let processFailure (error) = request.OwinContext |> handler.SerializeFailure error HttpStatusCode.BadRequest
        
        let processHandler (body) = 
            match handler.Process(request, body) with
            | SomeResponse(body, successCode, failureCode) -> 
                request.OwinContext |> handler.Serialize body successCode failureCode
            | SuccessResponse(body, successCode) -> request.OwinContext |> handler.SerializeSuccess body successCode
            | FailureResponse(body, failureCode) -> request.OwinContext |> handler.SerializeFailure body failureCode
            | NoResponse -> ()
        
        match validateRequest() with
        | Choice1Of2 body -> processHandler body
        | Choice2Of2 error -> processFailure error
    
    /// Default OWIN method to process request
    let exec (owin : IOwinContext) = 
        async { 
            let findHandler lookupValue = 
                match httpModule.TryGetValue(lookupValue) with
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
                | Some(handler) -> execute (owin.RequestContext, handler) |> ignore
                | None -> owin |> BAD_REQUEST(Response<unit>.WithError(HttpNotSupported))
            with __ -> ()
        }
    
    /// Default OWIN handler to transform C# function to F#
    let handler = Func<IOwinContext, Tasks.Task>(fun owin -> Async.StartAsTask(exec (owin)) :> Task)
    
    let mutable server = Unchecked.defaultof<IDisposable>
    let mutable thread = Unchecked.defaultof<_>
    member __.Configuration(app : IAppBuilder) = app.Run(handler)
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
