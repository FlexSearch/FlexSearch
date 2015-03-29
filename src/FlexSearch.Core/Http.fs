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

// ----------------------------------------------------------------------------
// Http related types and helpers
// ----------------------------------------------------------------------------
/// Http Handler properties to define the behaviour of
/// Http Rest web service
type HttpHandlerProperties = 
    { /// Do not do any processing and let the
      /// target method do all the processing
      FullControl : bool
      /// Throw error if the request is missing 
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
        { FullControl = true
          FailOnMissingBody = false
          CheckIndexExists = false
          CheckIndexIsOnline = false
          ValidateDto = false
          SetValuesFromQueryString = false }
    
    static member OnlineIndex = 
        { FullControl = false
          FailOnMissingBody = true
          CheckIndexExists = true
          CheckIndexIsOnline = true
          ValidateDto = true
          SetValuesFromQueryString = true }
    
    static member IndexExists = 
        { FullControl = false
          FailOnMissingBody = true
          CheckIndexExists = true
          CheckIndexIsOnline = false
          ValidateDto = true
          SetValuesFromQueryString = true }

/// HTTP resource to handle incoming requests
type IHttpResource = 
    abstract Properties : HttpHandlerProperties
    abstract Execute : id:option<string> * subid:option<string> * context:IOwinContext -> unit

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

[<AutoOpen>]
module HttpHelpers = 
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
    
    /// This is responsible for generating routing lookup table from the registerd http modules
    let generateRoutingTable (modules : Dictionary<string, IHttpResource>) = 
        let result = new Dictionary<string, IHttpResource>(StringComparer.OrdinalIgnoreCase)
        for m in modules do
            // check if the key supports more than one http verb
            if m.Key.Contains("|") then 
                let verb = m.Key.Substring(0, m.Key.IndexOf("-"))
                let verbs = verb.Split([| '|' |], StringSplitOptions.RemoveEmptyEntries)
                let value = m.Key.Substring(m.Key.IndexOf("-") + 1)
                for v in verbs do
                    result.Add(v + "-" + value, m.Value)
            else result.Add(m.Key, m.Value)
        result

/// Handler base class which exposes common Http Handler functionality
[<AbstractClass>]
type HttpHandlerBase<'T, 'U when 'T :> IValidate>(?properties0 : HttpHandlerProperties) = 
    let properties = defaultArg properties0 HttpHandlerProperties.OnlineIndex
    
    let hasBody = 
        if typeof<'T> = typeof<unit> then false
        else true
    
    member __.Deserialize(request : IOwinRequest) = getRequestBody<'T> (request)
    
    member __.Serialize (response : Choice<'U, Error>) (successStatus : HttpStatusCode) (failureStatus : HttpStatusCode) 
           (owinContext : IOwinContext) = 
        match response with
        | Choice1Of2(r) -> 
            let instance = Response<'U>.WithData(r)
            owinContext |> writeResponse successStatus instance
        | Choice2Of2(r) -> 
            let instance = Response<'U>.WithError(r)
            owinContext |> writeResponse failureStatus instance
    
    abstract Process : id:option<string> * subId:option<string> * body:Option<'T> * context:IOwinContext
     -> Choice<'U, Error> * HttpStatusCode * HttpStatusCode
    override __.Process(_, _, _, _) = (fail HttpNotSupported, HttpStatusCode.OK, HttpStatusCode.BadRequest)
    abstract Process : context:IOwinContext -> unit
    override __.Process(context) = context |> BAD_REQUEST Errors.HTTP_NOT_SUPPORTED
    interface IHttpResource with
        member __.Properties = properties
        member this.Execute(id, subId, context) = 
            if properties.FullControl then this.Process(context)
            else if hasBody then 
                match this.Deserialize(context.Request) with
                | Choice1Of2(body) -> 
                    let (response, successCode, failureCode) = this.Process(id, subId, (Some(body)), context)
                    context |> this.Serialize (response) successCode failureCode
                | Choice2Of2(e) -> 
                    if properties.FailOnMissingBody then 
                        context |> this.Serialize (fail e) HttpStatusCode.OK HttpStatusCode.BadRequest
                    else 
                        let (response, successCode, failureCode) = this.Process(id, subId, None, context)
                        context |> this.Serialize (response) successCode failureCode
            else 
                let (response, successCode, failureCode) = this.Process(id, subId, None, context)
                context |> this.Serialize (response) successCode failureCode

type IServer = 
    abstract Start : unit -> unit
    abstract Stop : unit -> unit

/// Owin katana server
[<Sealed>]
type OwinServer(indexExists : string -> bool, modules : Dictionary<string, IHttpResource>, logger : ILogService, ?port0 : int) = 
    let port = defaultArg port0 9800
    let accessDenied = """
Port access issue. Make sure that the running user has necessary permission to open the port. 
Use the below command to add URL reservation.
---------------------------------------------------------------------------
netsh http add urlacl url=http://+:{port}/ user=everyone listen=yes
---------------------------------------------------------------------------
"""
    
    let httpModule = 
        let result = new Dictionary<string, IHttpResource>(StringComparer.OrdinalIgnoreCase)
        for m in modules do
            // check if the key supports more than one http verb
            if m.Key.Contains("|") then 
                let verb = m.Key.Substring(0, m.Key.IndexOf("-"))
                let verbs = verb.Split([| '|' |], StringSplitOptions.RemoveEmptyEntries)
                let value = m.Key.Substring(m.Key.IndexOf("-") + 1)
                for v in verbs do
                    result.Add(v + "-" + value, m.Value)
            else result.Add(m.Key, m.Value)
        result
    
    /// Default OWIN method to process request
    let exec (owin : IOwinContext) = 
        async { 
            let getModule lookupValue (id : option<string>) (subId : option<string>) (owin : IOwinContext) = 
                match httpModule.TryGetValue(owin.Request.Method.ToLowerInvariant() + "-" + lookupValue) with
                | (true, x) -> x.Execute(id, subId, owin)
                | _ -> owin |> BAD_REQUEST(Response<unit>.WithError(HttpNotSupported))
            
            let matchSubModules (id : string, x : int, owin : IOwinContext) = 
                match x with
                | 3 -> getModule ("/" + owin.Request.Uri.Segments.[1] + ":id") (Some(id)) None owin
                | 4 -> 
                    getModule 
                        ("/" + owin.Request.Uri.Segments.[1] + ":id/" 
                         + removeTrailingSlash owin.Request.Uri.Segments.[3]) (Some(id)) None owin
                | 5 -> 
                    getModule ("/" + owin.Request.Uri.Segments.[1] + ":id/" + owin.Request.Uri.Segments.[3] + ":id") 
                        (Some(id)) (Some(owin.Request.Uri.Segments.[4])) owin
                | _ -> owin |> BAD_REQUEST(Response<unit>.WithError(HttpNotSupported))
            
            try 
                match owin.Request.Uri.Segments.Length with
                // Server root
                | 1 -> getModule "/" None None owin
                // Root resource request
                | 2 -> getModule ("/" + removeTrailingSlash owin.Request.Uri.Segments.[1]) None None owin
                | x when x > 2 && x <= 5 -> 
                    let id = removeTrailingSlash owin.Request.Uri.Segments.[2]
                    // Check if the Uri is indices and perform an index exists check
                    if (String.Equals(owin.Request.Uri.Segments.[1], "indices") 
                        || String.Equals(owin.Request.Uri.Segments.[1], "indices/")) then 
                        match indexExists (id) with
                        | true -> matchSubModules (id, x, owin)
                        | false -> owin |> NOT_FOUND(Response<unit>.WithError(IndexNotFound id))
                    else matchSubModules (id, x, owin)
                | _ -> owin |> BAD_REQUEST(Response<unit>.WithError(HttpNotSupported))
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
