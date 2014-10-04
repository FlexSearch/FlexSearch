// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

[<AutoOpen>]
module HttpHelpers = 
    open FlexSearch
    open FlexSearch.Api
    open FlexSearch.Api.Messages
    open FlexSearch.Utility
    open Microsoft.Owin
    open Newtonsoft.Json
    open Newtonsoft.Json.Converters
    open ProtoBuf
    open System
    open System.Collections.Concurrent
    open System.IO
    open System.Linq
    open System.Net
    open System.Text
    open System.Threading
    
    /// Helper method to serialize cluster messages
    let ProtoSerialize(message : 'a) = 
        use stream = new MemoryStream()
        Serializer.Serialize(stream, message)
        stream.ToArray()
    
    /// Helper method to De-serialize cluster messages
    let ProtoDeserialize<'T>(message : byte []) = 
        use stream = new MemoryStream(message)
        Serializer.Deserialize<'T>(stream)
    
    /// <summary>
    /// Formatter interface for supporting multiple formats in the HTTP engine
    /// </summary>
    type IFormatter = 
        abstract SupportedHeaders : unit -> string []
        abstract Serialize : body:obj * stream:Stream -> unit
        abstract SerializeToString : body:obj -> string
        abstract DeSerialize<'T> : stream:Stream -> 'T
    
    [<Sealed>]
    type JilJsonFormatter() = 
        let options = new Jil.Options(false, false, true, Jil.DateTimeFormat.ISO8601, false)
        interface IFormatter with
            member x.SerializeToString(body : obj) = failwith "Not implemented yet"
            
            member x.DeSerialize<'T>(stream : Stream) = 
                use reader = new StreamReader(stream) :> TextReader
                Jil.JSON.Deserialize<'T>(reader, options)
            
            member x.Serialize(body : obj, stream : Stream) : unit = 
                use writer = new StreamWriter(stream)
                Jil.JSON.Serialize(body, writer, options)
                writer.Flush()
            
            member x.SupportedHeaders() : string [] = 
                [| "application/json"; "text/json"; "application/json;charset=utf-8"; "application/json; charset=utf-8" |]
    
    [<Sealed>]
    type NewtonsoftJsonFormatter() = 
        let options = new Newtonsoft.Json.JsonSerializerSettings()
        do options.Converters.Add(new StringEnumConverter())
        interface IFormatter with
            member x.SerializeToString(body : obj) = failwith "Not implemented yet"
            
            member x.DeSerialize<'T>(stream : Stream) = 
                use reader = new StreamReader(stream)
                JsonConvert.DeserializeObject<'T>(reader.ReadToEnd(), options)
            
            member x.Serialize(body : obj, stream : Stream) : unit = 
                use writer = new StreamWriter(stream)
                let body = JsonConvert.SerializeObject(body, options)
                writer.Write(body)
                writer.Flush()
            
            member x.SupportedHeaders() : string [] = 
                [| "application/json"; "text/json"; "application/json;charset=utf-8"; "application/json; charset=utf-8" |]
    
    [<Sealed>]
    type ProtoBufferFormatter() = 
        interface IFormatter with
            member x.SerializeToString(body : obj) = failwith "Not implemented yet"
            member x.DeSerialize<'T>(stream : Stream) = ProtoBuf.Serializer.Deserialize<'T>(stream)
            member x.Serialize(body : obj, stream : Stream) : unit = ProtoBuf.Serializer.Serialize(stream, body)
            member x.SupportedHeaders() : string [] = [| "application/x-protobuf"; "application/octet-stream" |]
    
    [<Sealed>]
    type YamlFormatter() = 
        let options = YamlDotNet.Serialization.SerializationOptions.EmitDefaults
        let serializer = new YamlDotNet.Serialization.Serializer(options)
        let deserializer = new YamlDotNet.Serialization.Deserializer(ignoreUnmatched = true)
        interface IFormatter with
            
            member x.SerializeToString(body : obj) = 
                use textWriter = new StringWriter()
                serializer.Serialize(textWriter, body)
                textWriter.ToString()
            
            member x.DeSerialize<'T>(stream : Stream) = 
                use textReader = new StreamReader(stream)
                deserializer.Deserialize<'T>(textReader)
            
            member x.Serialize(body : obj, stream : Stream) : unit = 
                use TextWriter = new StreamWriter(stream)
                serializer.Serialize(TextWriter, body)
            
            member x.SupportedHeaders() : string [] = [| "application/yaml" |]
    
    let private Formatters = new ConcurrentDictionary<string, IFormatter>(StringComparer.OrdinalIgnoreCase)
    let protoFormatter = new ProtoBufferFormatter() :> IFormatter
    let jsonFormatter = new NewtonsoftJsonFormatter() :> IFormatter
    
    protoFormatter.SupportedHeaders() |> Array.iter (fun x -> Formatters.TryAdd(x, protoFormatter) |> ignore)
    jsonFormatter.SupportedHeaders() |> Array.iter (fun x -> Formatters.TryAdd(x, jsonFormatter) |> ignore)
    
    /// Get request format from the request object
    /// Defaults to json
    let private GetRequestFormat(request : IOwinRequest) = 
        if String.IsNullOrWhiteSpace(request.ContentType) then "application/json"
        else request.ContentType
    
    /// Get response format from the OWIN context
    /// Defaults to json
    let private GetResponseFormat(owin : IOwinContext) = 
        if owin.Request.Accept = null then "application/json"
        else if owin.Request.Accept = "*/*" then "application/json"
        else if owin.Request.Accept.Contains(",") then 
            owin.Request.Accept.Substring(0, owin.Request.Accept.IndexOf(","))
        else owin.Request.Accept
    
    /// Write HTTP response
    let WriteResponse (statusCode : System.Net.HttpStatusCode) (response : obj) (owin : IOwinContext) = 
        owin.Response.StatusCode <- int statusCode
        let format = GetResponseFormat owin
        owin.Response.ContentType <- format
        if response <> Unchecked.defaultof<_> then 
            match Formatters.TryGetValue(format) with
            | true, formatter -> formatter.Serialize(response, owin.Response.Body)
            | _ -> owin.Response.StatusCode <- int HttpStatusCode.InternalServerError
    
    let inline IsNull(x) = obj.ReferenceEquals(x, Unchecked.defaultof<_>)
    
    /// Write HTTP response
    let GetRequestBody<'T>(request : IOwinRequest) = 
        let contentType = GetRequestFormat request
        if request.Body.CanRead then 
            match Formatters.TryGetValue(contentType) with
            | true, formatter -> 
                try 
                    let result = formatter.DeSerialize<'T>(request.Body)
                    if IsNull result then 
                        Choice2Of2(Errors.HTTP_UNABLE_TO_PARSE
                                   |> GenerateOperationMessage
                                   |> Append("Message", "No body is defined."))
                    else Choice1Of2(result)
                with ex -> 
                    Choice2Of2(Errors.HTTP_UNABLE_TO_PARSE
                               |> GenerateOperationMessage
                               |> Append("Message", ex.Message))
            | _ -> Choice2Of2(Errors.HTTP_UNSUPPORTED_CONTENT_TYPE |> GenerateOperationMessage)
        else Choice2Of2(Errors.HTTP_NO_BODY_DEFINED |> GenerateOperationMessage)
    
    let Created = HttpStatusCode.Created
    let Accepted = HttpStatusCode.Accepted
    let Ok = HttpStatusCode.OK
    let NotFound = HttpStatusCode.NotFound
    let BadRequest = HttpStatusCode.BadRequest
    let Conflict = HttpStatusCode.Conflict
    let CREATED (value : obj) (owin : IOwinContext) = WriteResponse HttpStatusCode.Created value owin
    let ACCEPTED (value : obj) (owin : IOwinContext) = WriteResponse HttpStatusCode.Accepted value owin
    let OK (value : obj) (owin : IOwinContext) = WriteResponse HttpStatusCode.OK value owin
    let BAD_REQUEST (value : obj) (owin : IOwinContext) = WriteResponse HttpStatusCode.BadRequest value owin
    let NOT_FOUND (value : obj) (owin : IOwinContext) = WriteResponse HttpStatusCode.NotFound value owin
    let CONFLICT (value : obj) (owin : IOwinContext) = WriteResponse HttpStatusCode.Conflict value owin
    
    let GetValueFromQueryString key defaultValue (owin : IOwinContext) = 
        match owin.Request.Query.Get(key) with
        | null -> defaultValue
        | value -> value
    
    let GetIntValueFromQueryString key defaultValue (owin : IOwinContext) = 
        match owin.Request.Query.Get(key) with
        | null -> defaultValue
        | value -> 
            match Int32.TryParse(value) with
            | true, v' -> v'
            | _ -> defaultValue
    
    let GetBoolValueFromQueryString key defaultValue (owin : IOwinContext) = 
        match owin.Request.Query.Get(key) with
        | null -> defaultValue
        | value -> 
            match Boolean.TryParse(value) with
            | true, v' -> v'
            | _ -> defaultValue
    
    let inline CheckIdPresent(owin : IOwinContext) = 
        if owin.Request.Uri.Segments.Length >= 4 then Some(owin.Request.Uri.Segments.[3])
        else None
    
    let ResponseProcessor (f : Choice<'T, OperationMessage>) success failure (owin : IOwinContext) = 
        // For parameter less constructor the performance of Activator is as good as direct initialization. Based on the 
        // finding of http://geekswithblogs.net/mrsteve/archive/2012/02/11/c-sharp-performance-new-vs-expression-tree-func-vs-activator.createinstance.aspx
        // In future we can cache it if performance is found to be an issue.
        let instance = Activator.CreateInstance<Response<'T>>()
        match f with
        | Choice1Of2(r) -> 
            instance.Data <- r
            success instance owin
        | Choice2Of2(r) -> 
            instance.Error <- r
            failure instance owin
    
    let inline RemoveTrailingSlash(input : string) = 
        if input.EndsWith("/") then input.Substring(0, (input.Length - 1))
        else input
    
    let inline GetIndexName(owin : IOwinContext) = RemoveTrailingSlash owin.Request.Uri.Segments.[2]
    let inline SubId(owin : IOwinContext) = RemoveTrailingSlash owin.Request.Uri.Segments.[4]
