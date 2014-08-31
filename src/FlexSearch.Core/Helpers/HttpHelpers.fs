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
    open FlexSearch.Core
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
    open FlexSearch.Api.Messages
    
    let jsonSettings = new JsonSerializerSettings()
    
    jsonSettings.Converters.Add(new StringEnumConverter())
    
    /// Helper method to serialize cluster messages
    let ProtoSerialize(message : 'a) = 
        use stream = new MemoryStream()
        Serializer.Serialize(stream, message)
        stream.ToArray()
    
    /// Helper method to deserialize cluster messages
    let ProtoDeserialize<'T>(message : byte []) = 
        use stream = new MemoryStream(message)
        Serializer.Deserialize<'T>(stream)
    
    /// Get request format from the request object
    /// Defaults to json
    let private GetRequestFormat(request : IOwinRequest) = 
        if String.IsNullOrWhiteSpace(request.ContentType) then "application/json"
        else request.ContentType
    
    /// Get response format from the owin context
    /// Defaults to json
    let private GetResponseFormat(owin : IOwinContext) = 
        if owin.Request.Accept = null then "application/json"
        else if owin.Request.Accept = "*/*" then "application/json"
        else if owin.Request.Accept.Contains(",") then 
            owin.Request.Accept.Substring(0, owin.Request.Accept.IndexOf(","))
        else owin.Request.Accept
    
    /// Write http response
    let WriteResponse (statusCode : System.Net.HttpStatusCode) (response : obj) (owin : IOwinContext) = 
        let matchType format res = 
            match format with
            | "text/json" | "application/json" | "json" -> 
                owin.Response.ContentType <- "text/json"
                match owin.Request.Query.Get("callback") with
                | null -> 
                    let result = JsonConvert.SerializeObject(res, jsonSettings)
                    Some(Encoding.UTF8.GetBytes(result))
                | fname -> 
                    let result = sprintf "%s(%s);" fname (JsonConvert.SerializeObject(res, jsonSettings))
                    Some(Encoding.UTF8.GetBytes(result))
            | "application/x-protobuf" | "application/octet-stream" | "proto" -> 
                owin.Response.ContentType <- "application/x-protobuf"
                Some(ProtoSerialize(res))
            | _ -> None
        owin.Response.StatusCode <- int statusCode
        if response <> Unchecked.defaultof<_> then 
            let format = GetResponseFormat owin
            let result = matchType format response
            match result with
            | None -> owin.Response.StatusCode <- int HttpStatusCode.InternalServerError
            | Some(x) -> await (owin.Response.WriteAsync(x))
    
    let inline IsNull(x) = obj.ReferenceEquals(x, Unchecked.defaultof<_>)
    
    /// Write http response
    let GetRequestBody<'T>(request : IOwinRequest) = 
        let contentType = GetRequestFormat request
        if request.Body.CanRead then 
            match contentType with
            | "text/json" | "application/json" | "application/json; charset=utf-8" | "application/json;charset=utf-8" | "json" -> 
                let body = 
                    use reader = new System.IO.StreamReader(request.Body)
                    reader.ReadToEnd()
                if String.IsNullOrWhiteSpace(body) <> true then 
                    try 
                        let result = JsonConvert.DeserializeObject<'T>(body)
                        if IsNull result then 
                            Choice2Of2(Errors.HTTP_UNABLE_TO_PARSE
                                       |> GenerateOperationMessage
                                       |> Append("Message", "No body is defined."))
                        else Choice1Of2(result)
                    with ex -> 
                        Choice2Of2(Errors.HTTP_UNABLE_TO_PARSE
                                   |> GenerateOperationMessage
                                   |> Append("Message", ex.Message))
                else Choice2Of2(Errors.HTTP_NO_BODY_DEFINED |> GenerateOperationMessage)
            | "application/x-protobuf" | "application/octet-stream" | "proto" -> 
                try 
                    Choice1Of2(ProtoBuf.Serializer.Deserialize<'T>(request.Body))
                with ex -> 
                    Choice2Of2(Errors.HTTP_UNABLE_TO_PARSE
                               |> GenerateOperationMessage
                               |> Append("Message", ex.Message))
            | _ -> Choice2Of2(Errors.HTTP_UNSUPPORTED_CONTENT_TYPE |> GenerateOperationMessage)
        else Choice2Of2(Errors.HTTP_NO_BODY_DEFINED |> GenerateOperationMessage)
    
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
