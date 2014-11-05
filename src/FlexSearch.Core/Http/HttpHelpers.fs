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

open FlexSearch
open FlexSearch.Api
open FlexSearch.Api.Messages
open Microsoft.Owin
open System
open System.Collections.Concurrent
open System.Net

[<AutoOpen>]
module HttpHelpers = 
    let private Formatters = new ConcurrentDictionary<string, IFormatter>(StringComparer.OrdinalIgnoreCase)
    let protoFormatter = new ProtoBufferFormatter() :> IFormatter
    let jsonFormatter = new NewtonsoftJsonFormatter() :> IFormatter
    
    protoFormatter.SupportedHeaders() |> Array.iter (fun x -> Formatters.TryAdd(x, protoFormatter) |> ignore)
    jsonFormatter.SupportedHeaders() |> Array.iter (fun x -> Formatters.TryAdd(x, jsonFormatter) |> ignore)
    
    /// Get request format from the request object
    /// Defaults to JSON
    let private GetRequestFormat(request : IOwinRequest) = 
        if String.IsNullOrWhiteSpace(request.ContentType) then "application/json"
        else request.ContentType
    
    /// Get response format from the OWIN context
    /// Defaults to JSON
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
            | true, formatter -> formatter.Serialize(response, owin)
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
    let BAD_REQUEST (value : obj) (owin : IOwinContext) = WriteResponse HttpStatusCode.BadRequest value owin
    let NOT_FOUND (value : obj) (owin : IOwinContext) = WriteResponse HttpStatusCode.NotFound value owin
    
    let GetValueFromQueryString key defaultValue (owin : IOwinContext) = 
        match owin.Request.Query.Get(key) with
        | null -> defaultValue
        | value -> value
    
    let GetValueFromQueryString1 key (owin : IOwinContext) = 
        match owin.Request.Query.Get(key) with
        | null -> 
            Choice2Of2(Errors.MISSING_FIELD_VALUE
                       |> GenerateOperationMessage
                       |> Append("Parameter", key))
        | value -> Choice1Of2(value)
    
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
    
    let GetDateTimeValueFromQueryString key (owin : IOwinContext) = 
        match owin.Request.Query.Get(key) with
        | null -> 
            Choice2Of2(Errors.MISSING_FIELD_VALUE
                       |> GenerateOperationMessage
                       |> Append("Parameter", key))
        | value -> 
            match Int64.TryParse(value) with
            | true, v' -> Choice1Of2(v')
            | _ -> 
                Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                           |> GenerateOperationMessage
                           |> Append("Parameter", key))
    
    let inline CheckIdPresent(owin : IOwinContext) = 
        if owin.Request.Uri.Segments.Length >= 4 then Some(owin.Request.Uri.Segments.[3])
        else None
    
    let inline RemoveTrailingSlash(input : string) = 
        if input.EndsWith("/") then input.Substring(0, (input.Length - 1))
        else input
    
    let inline GetIndexName(owin : IOwinContext) = RemoveTrailingSlash owin.Request.Uri.Segments.[2]
    let inline SubId(owin : IOwinContext) = RemoveTrailingSlash owin.Request.Uri.Segments.[4]

/// <summary>
/// HTTP resource to handle incoming requests
/// </summary>
type IHttpResource = 
    abstract TakeFullControl : bool
    abstract HasBody : bool
    abstract FailOnMissingBody : bool
    abstract Execute : id:option<string> * subid:option<string> * context:IOwinContext -> unit

/// <summary>
/// Handler base class which exposes common Http Handler functionality
/// </summary>
[<AbstractClass>]
type HttpHandlerBase<'T, 'U>(?failOnMissingBody0 : bool, ?fullControl0 : bool) = 
    let failOnMissingBody = defaultArg failOnMissingBody0 true
    let fullControl = defaultArg fullControl0 false
    
    let hasBody = 
        if typeof<'T> = typeof<unit> then false
        else true
    
    member this.Deserialize(request : IOwinRequest) = GetRequestBody<'T>(request)
    
    member this.Serialize (response : Choice<'U, OperationMessage>) (successStatus : HttpStatusCode) 
           (failureStatus : HttpStatusCode) (owinContext : IOwinContext) = 
        // For parameter less constructor the performance of Activator is as good as direct initialization. Based on the 
        // finding of http://geekswithblogs.net/mrsteve/archive/2012/02/11/c-sharp-performance-new-vs-expression-tree-func-vs-activator.createinstance.aspx
        // In future we can cache it if performance is found to be an issue.
        let instance = Activator.CreateInstance<Response<'U>>()
        match response with
        | Choice1Of2(r) -> 
            instance.Data <- r
            WriteResponse successStatus instance owinContext
        | Choice2Of2(r) -> 
            instance.Error <- r
            WriteResponse failureStatus instance owinContext
    
    abstract Process : id:option<string> * subId:option<string> * body:Option<'T> * context:IOwinContext
     -> Choice<'U, OperationMessage> * HttpStatusCode * HttpStatusCode
    override this.Process(id, subId, body, context) = 
        (Choice2Of2(Errors.HTTP_NOT_SUPPORTED |> GenerateOperationMessage), HttpStatusCode.OK, HttpStatusCode.BadRequest)
    abstract Process : context:IOwinContext -> unit
    override this.Process(context) = context |> BAD_REQUEST Errors.HTTP_NOT_SUPPORTED
    interface IHttpResource with
        member this.TakeFullControl = fullControl
        member this.FailOnMissingBody = failOnMissingBody
        member this.HasBody = hasBody
        member this.Execute(id, subId, context) = 
            if fullControl then this.Process(context)
            else if hasBody then 
                match this.Deserialize(context.Request) with
                | Choice1Of2(body) -> 
                    let (response, successCode, failureCode) = this.Process(id, subId, (Some(body)), context)
                    context |> this.Serialize (response) successCode failureCode
                | Choice2Of2(e) -> 
                    if failOnMissingBody then 
                        context |> this.Serialize (Choice2Of2(e)) HttpStatusCode.OK HttpStatusCode.BadRequest
                    else 
                        let (response, successCode, failureCode) = this.Process(id, subId, None, context)
                        context |> this.Serialize (response) successCode failureCode
            else 
                let (response, successCode, failureCode) = this.Process(id, subId, None, context)
                context |> this.Serialize (response) successCode failureCode
