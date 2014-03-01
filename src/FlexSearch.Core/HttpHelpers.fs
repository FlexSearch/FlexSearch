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
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

// ----------------------------------------------------------------------------
module HttpHelpers = 
    open FlexSearch
    open FlexSearch.Api
    open FlexSearch.Api.Message
    open FlexSearch.Core
    open FlexSearch.Utility
    open Microsoft.Owin
    open Newtonsoft.Json
    open ProtoBuf
    open System
    open System.Collections.Concurrent
    open System.IO
    open System.Linq
    open System.Net
    open System.Text
    open System.Threading
    open Newtonsoft.Json
    open Newtonsoft.Json.Converters
    
    let jsonSettings = new JsonSerializerSettings()
    jsonSettings.Converters.Add(new StringEnumConverter())

    /// Helper method to serialize cluster messages
    let protoSerialize (message : 'a) = 
        use stream = new MemoryStream()
        Serializer.Serialize(stream, message)
        stream.ToArray()
    
    /// Helper method to deserialize cluster messages
    let protoDeserialize<'T> (message : byte []) = 
        use stream = new MemoryStream(message)
        Serializer.Deserialize<'T>(stream)
    
    /// Write http response
    let writeResponse (statusCode : System.Net.HttpStatusCode) (res : obj) (owin : IOwinContext) = 
        let matchType format res = 
            match format with
            | "text/json" | "application/json" | "json" -> 
                owin.Response.ContentType <- "text/json"
                let result = JsonConvert.SerializeObject(res, jsonSettings)
                Some(Encoding.UTF8.GetBytes(result))
            | "application/x-protobuf" | "application/octet-stream" | "proto" -> 
                owin.Response.ContentType <- "application/x-protobuf"
                Some(protoSerialize (res))
            | _ -> None
        
        let result = 
            if owin.Request.Uri.Segments.Last().Contains(".") then 
                matchType (owin.Request.Uri.Segments.Last().Substring(owin.Request.Uri.Segments.Last().IndexOf(".") + 1)) 
                    res
            else if owin.Request.Accept = null then matchType owin.Request.ContentType res
            else 
                if owin.Request.Accept.Contains(",") then
                    let header = owin.Request.Accept.Substring(0, owin.Request.Accept.IndexOf(","))
                    matchType header res
                else
                    matchType owin.Request.Accept res
        
        owin.Response.StatusCode <- int statusCode
        match result with
        | None -> owin.Response.StatusCode <- int HttpStatusCode.NotAcceptable
        | Some(x) -> await (owin.Response.WriteAsync(x))
    
    /// Write http response
    let getRequestBody<'T when 'T : null> (request : IOwinRequest) = 
        if request.Body.CanRead then 
            match request.ContentType with
            | "text/json" | "application/json" -> 
                let body = 
                    use reader = new System.IO.StreamReader(request.Body)
                    reader.ReadToEnd()
                try 
                    match JsonConvert.DeserializeObject<'T>(body) with
                    | null -> Choice2Of2(OperationMessage.WithDeveloperMessage(MessageConstants.HTTP_UNABLE_TO_PARSE, "No body is defined."))
                    | result -> Choice1Of2(result)
                with ex -> Choice2Of2(OperationMessage.WithDeveloperMessage(MessageConstants.HTTP_UNABLE_TO_PARSE, ex.Message))
            | "application/x-protobuf" | "application/octet-stream" -> 
                try 
                    Choice1Of2(ProtoBuf.Serializer.Deserialize<'T>(request.Body))
                with ex -> Choice2Of2(OperationMessage.WithDeveloperMessage(MessageConstants.HTTP_UNABLE_TO_PARSE, ex.Message))
            | _ -> Choice2Of2(MessageConstants.HTTP_UNSUPPORTED_CONTENT_TYPE)
        else Choice2Of2(MessageConstants.HTTP_NO_BODY_DEFINED)
    
    let OK (value : obj) (owin : IOwinContext) = writeResponse HttpStatusCode.OK value owin
    let BAD_REQUEST (value : obj) (owin : IOwinContext) = writeResponse HttpStatusCode.BadRequest value owin
    let getValueFromQueryString key defaultValue (owin : IOwinContext) = owin.Request.Query.Get(key)
    
    let inline checkIdPresent (owin : IOwinContext) = 
        if owin.Request.Uri.Segments.Length >= 4 then Some(owin.Request.Uri.Segments.[3])
        else None
    
    let responseProcessor (f : Choice<'T, 'U>) success failure (owin : IOwinContext) = 
        match f with
        | Choice1Of2(r) -> success r owin
        | Choice2Of2(r) -> failure r owin
    
    type HttpBuilder() = 
        
        member this.Bind(v, f) = 
            match v with
            | Choice1Of2(x) -> f x
            | Choice2Of2(s) -> Choice2Of2(s)
        
        member this.ReturnFrom v = v
        member this.Return v = Choice1Of2(v)
        member this.Zero() = Choice1Of2()
