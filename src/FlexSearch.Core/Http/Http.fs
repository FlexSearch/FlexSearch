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

open FlexSearch.Api
open FlexSearch.Api.Model
open FlexSearch.Core
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Net
open System.Reflection
open System.Threading
open System.Threading.Tasks
open System.Runtime.Versioning
open System.ComponentModel.Composition
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Hosting.Internal
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.StaticFiles
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Cors
open Microsoft.Extensions.Primitives
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.PlatformAbstractions
open Microsoft.Extensions.Logging

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
          HttpContext : HttpContext }
        static member Create(owinContext, resName, ?resId, ?subResName, ?subResId) = 
            { ResName = resName
              ResId = resId
              SubResName = subResName
              SubResId = subResId
              HttpContext = owinContext }
    
    type ResponseContext<'T> = 
        | SomeResponse of responseBody : Result<'T> * successCode : HttpStatusCode * failureCode : HttpStatusCode
        | SuccessResponse of responseBody : 'T * successCode : HttpStatusCode
        | FailureResponse of responseBody : IMessage * failureCode : HttpStatusCode
        | FailureOpMsgResponse of responseBody : OperationMessage * failureCode : HttpStatusCode
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
        static member WithError(error : IMessage) = 
            { Data = Unchecked.defaultof<'T>
              Error = error.OperationMessage() }
    
    let private Formatters = new ConcurrentDictionary<string, IFormatter>(StringComparer.OrdinalIgnoreCase)
    let jsonFormatter = new NewtonsoftJsonFormatter() :> IFormatter
    
    jsonFormatter.SupportedHeaders |> Array.iter (fun x -> Formatters.TryAdd(x, jsonFormatter) |> ignore)
    
    /// Get request format from the request object
    /// Defaults to JSON
    let private getRequestFormat (request : HttpRequest) = 
        if String.IsNullOrWhiteSpace(request.ContentType) then "application/json"
        else request.ContentType
    
    /// Get response format from the OWIN context
    /// Defaults to JSON
    let private getResponseFormat (ctxt : HttpContext) = 
        let acceptHeader = ctxt.Request.Headers |> getFirstStringValue "Accept"
        match acceptHeader with
        | null | "*/*" -> 
            match ctxt.Request.Query |> getFirstStringValue "callback" with
            | null -> "application/json"
            | _ -> "application/javascript"
        | x when x.Contains(",") -> acceptHeader.Split(',').[0]
        | x -> x
    
    /// Write HTTP response
    let writeResponse (statusCode : System.Net.HttpStatusCode) (response : obj) (ctxt : HttpContext) = 
        ctxt.Response.StatusCode <- int statusCode
        let format = getResponseFormat ctxt
        ctxt.Response.ContentType <- format
        if response <> Unchecked.defaultof<_> then 
            match Formatters.TryGetValue(format) with
            | true, formatter -> 
                match format with
                // Handle the special jsonp case
                | "application/javascript" -> 
                    use streamWriter = new StreamWriter(ctxt.Response.Body)
                    streamWriter.Write((ctxt.Request.Query.Item "callback").Item 0)
                    streamWriter.Write "("
                    streamWriter.Flush()
                    jsonFormatter.Serialize(response, streamWriter.BaseStream)
                    streamWriter.Write ");"
                | _ -> formatter.Serialize(response, ctxt.Response.Body)
                // There isn't a need to flush the stream any more because it is already
                // closed by the Serializer.
                //try ctxt.Response.Body.Flush() with _ -> ()
            | _ -> 
                Logger.Log("Couldn't find a formatter for format: " + format, MessageKeyword.Default, MessageLevel.Error)
                ctxt.Response.StatusCode <- int HttpStatusCode.InternalServerError
    
    /// Write HTTP response
    let getRequestBody<'T> (request : HttpRequest) = 
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
    let BAD_REQUEST (value : obj) (ctx : HttpContext) = writeResponse HttpStatusCode.BadRequest value ctx
    let NOT_FOUND (value : obj) (ctx : HttpContext) = writeResponse HttpStatusCode.NotFound value ctx
    
    let inline removeTrailingSlash (input : string) = 
        if input.EndsWith("/") then input.Substring(0, (input.Length - 1))
        else input
    
    let inline getUri (req : HttpRequest) =
        new Uri(sprintf "%s://%s%s" req.Scheme req.Host.Value <| req.Path.ToUriComponent())

    /// A helper interface to dynamically find all the HttpHandlerBase classes
    type IHttpHandler = 
        abstract Execute : RequestContext -> unit
    
    /// Handler base class which exposes common Http Handler functionality
    [<AbstractClass>]
    type HttpHandlerBase<'T, 'U when 'T :> IDataTransferObject>(?failOnMissingBody : bool, ?validateBody : bool) = 
        member __.HasBody = typeof<'T> <> typeof<FlexSearch.Api.Model.NoBody>
        member this.FailOnMissingBody = defaultArg failOnMissingBody this.HasBody
        member __.ValidateBody = defaultArg validateBody false
        member __.DeSerialize(request : HttpRequest) = getRequestBody<'T> (request)
        
        member __.SerializeSuccess (response : 'U) (successStatus : HttpStatusCode) (httpContext : HttpContext) = 
            let instance = Response<'U>.WithData(response)
            httpContext |> writeResponse successStatus instance
        
        member __.SerializeFailure (response : IMessage) (failureStatus : HttpStatusCode) (httpContext : HttpContext) = 
            let instance = Response<'U>.WithError(response)
            httpContext |> writeResponse failureStatus instance
        
        member this.Serialize (response : Result<'U>) (successStatus : HttpStatusCode) 
               (failureStatus : HttpStatusCode) (httpContext : HttpContext) = 
            match response with
            | Ok(r) -> httpContext |> this.SerializeSuccess r successStatus
            | Fail(r) -> httpContext |> this.SerializeFailure r failureStatus
        
        abstract Process : request:RequestContext * body:'T option -> ResponseContext<'U>
        interface IHttpHandler with
            member handler.Execute(request : RequestContext) : unit = 
                let validateRequest() = 
                    maybe { 
                        let! body = match handler.HasBody with
                                    | true -> 
                                        match handler.DeSerialize(request.HttpContext.Request) with
                                        | Ok a -> ok <| Some(a)
                                        | Fail b -> 
                                            if handler.FailOnMissingBody then fail <| b
                                            else ok <| None
                                    | false -> ok <| None
                        /// Validate the DTO
                        if body.IsSome && handler.ValidateBody then do! validate body.Value
                        return body
                    }
                
                let processFailure (error) = 
                    request.HttpContext |> handler.SerializeFailure error HttpStatusCode.BadRequest
                
                let processHandler (body) = 
                    match handler.Process(request, body) with
                    | SomeResponse(body, successCode, failureCode) -> 
                        request.HttpContext |> handler.Serialize body successCode failureCode
                    | SuccessResponse(body, successCode) -> 
                        request.HttpContext |> handler.SerializeSuccess body successCode
                    | FailureResponse(body, failureCode) -> 
                        request.HttpContext |> handler.SerializeFailure body failureCode
                    | FailureOpMsgResponse(opMsg, failureCode) ->
                        request.HttpContext |> writeResponse failureCode { Data = Unchecked.defaultof<'U>; Error = opMsg }
                    | NoResponse -> ()
                
                match validateRequest() with
                | Ok body -> processHandler body
                | Fail error -> processFailure error
    
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
    
    type HttpContext with
        member this.RequestContext = 
            let uri = getUri this.Request
            let seg n = uri.Segments.[n]
            let segWithoutSlash n = seg n |> removeTrailingSlash

            match uri.Segments.Length with
            | 1 -> RequestContext.Create(this, "/")
            | 2 -> RequestContext.Create(this, seg 1)
            | 3 -> RequestContext.Create(this, segWithoutSlash 1, seg 2)
            | 4 -> RequestContext.Create(this, seg 1, segWithoutSlash 2, seg 3)
            | 5 -> RequestContext.Create(this, seg 1, segWithoutSlash 2, seg 3, seg 4)
            | _ -> failwithf "Internal Error: FlexSearch does not support URI more than 5 segments."

    /// Default method to process request
    let requestProcessor (ctxt : HttpContext) (httpHandlers : Dictionary<string, IHttpHandler>) = 
        async { 
            let findHandler lookupValue = 
                match httpHandlers.TryGetValue(lookupValue) with
                | (true, x) -> Some x
                | _ -> None
            try 
                let uri = getUri ctxt.Request
                let httpHandler = 
                    match uri.Segments.Length with
                    | 1 -> findHandler (ctxt.Request.Method + "/") // Server root
                    | 2 -> findHandler (ctxt.Request.Method + "/" + uri.Segments.[1]) // /Resource
                    | 3 -> findHandler (ctxt.Request.Method + "/" + uri.Segments.[1] + ":id") // /Resource/:Id
                    | 4 -> findHandler (ctxt.Request.Method + "/" + uri.Segments.[1] + ":id/" + uri.Segments.[3]) // /Resource/:Id/command
                    | 5 -> findHandler (ctxt.Request.Method + "/" + uri.Segments.[1] + ":id/" + uri.Segments.[3] + ":id") // /Resource/:Id/SubResouce/:Id
                    | _ -> None
                let isAuthenticated = ctxt.User.Identity.IsAuthenticated
                match httpHandler with
                | Some(handler) -> handler.Execute(ctxt.RequestContext)
                | None -> ctxt |> BAD_REQUEST(Response<unit>.WithError(HttpNotSupported))
            with e -> Logger.Log(sprintf "An error occurred while doing a %s on the URL: %s"
                                         ctxt.Request.Method
                                         ctxt.Request.Path.Value,
                                 e, 
                                 MessageKeyword.Default, 
                                 MessageLevel.Warning)
                      // Last resort response. If this fails, then we return nothing to the user
                      try ctxt |> writeResponse HttpStatusCode.InternalServerError 
                                                (Response<unit>.WithError(InternalServerError(exceptionPrinter e)))
                      with _ -> ()
        }