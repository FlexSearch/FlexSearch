namespace FlexSearch.TestSupport

open FlexSearch.Client
open FlexSearch.Api
open FlexSearch.Api.Messages
open FlexSearch.Core
open FlexSearch.Utility
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Text
open System.Threading
open FlexSearch.TestSupport
open Autofac
open Xunit
open Xunit.Extensions
open Microsoft.Owin.Testing

[<AutoOpen>]
module RestHelpers = 
    // ----------------------------------------------------------------------------
    // Test request pattern
    // ----------------------------------------------------------------------------
    type RequestBuilder = 
        { RequestType : string
          Uri : string
          mutable RequestBody : string
          mutable Response : HttpResponseMessage
          Server : TestServer }
    
    /// <summary>
    /// Build a new http test request
    /// </summary>
    /// <param name="httpMethod"></param>
    /// <param name="uri"></param>
    /// <param name="server"></param>
    let request (httpMethod : string) (uri : string) (server : TestServer) = 
        let request = 
            { RequestType = httpMethod
              Uri = uri
              RequestBody = ""
              Response = null
              Server = server }
        request
    
    let withBody (body : string) (requestBuilder : RequestBuilder) = 
        requestBuilder.RequestBody <- body
        requestBuilder
    
    let execute (requestBuilder : RequestBuilder) = 
        match requestBuilder.RequestType with
        | "GET" -> requestBuilder.Response <- requestBuilder.Server.HttpClient.GetAsync(requestBuilder.Uri).Result
        | "POST" -> 
            let content = new StringContent(requestBuilder.RequestBody, Encoding.UTF8, "application/json")
            requestBuilder.Response <- requestBuilder.Server.HttpClient.PostAsync(requestBuilder.Uri, content).Result
        | "PUT" -> 
            let content = new StringContent(requestBuilder.RequestBody, Encoding.UTF8, "application/json")
            requestBuilder.Response <- requestBuilder.Server.HttpClient.PutAsync(requestBuilder.Uri, content).Result
        | "DELETE" -> requestBuilder.Response <- requestBuilder.Server.HttpClient.DeleteAsync(requestBuilder.Uri).Result
        | _ -> failwithf "Not supported"
        requestBuilder
    
    // ----------------------------------------------------------------------------
    // Test assertions
    // ----------------------------------------------------------------------------
    let responseStatusEquals (status : HttpStatusCode) (result : RequestBuilder) = 
        Assert.True
            (status.Equals(result.Response.StatusCode), 
             ("Status code does not match: " + result.Response.StatusCode.ToString()))
        result
    
    let responseContainsHeader (header : string) (value : string) (result : RequestBuilder) = 
        Assert.Equal<string>(value, result.Response.Headers.GetValues(header).First()) // "Header value does not match"
        result
    
    let responseMatches (select : string) (expected : string) (result : RequestBuilder) = 
        let value = JObject.Parse(result.Response.Content.ReadAsStringAsync().Result)
        Assert.Equal<string>(expected, value.SelectToken(select).ToString()) // "Response does not match"
        result
    
    let responseShouldContain (value : string) (result : RequestBuilder) = 
        Assert.True
            (result.Response.Content.ReadAsStringAsync().Result.Contains(value), 
             "Response does contain the required value")
        result
    
    let responseContainsProperty (group : string) (key : string) (property : string) (expected : string) 
        (result : RequestBuilder) = 
        let value = JObject.Parse(result.Response.Content.ReadAsStringAsync().Result)
        Assert.Equal<string>(expected, value.SelectToken(group).[key].[property].ToString()) //"Response does contain the required property"
        result
    
    let responseBodyIsNull (result : RequestBuilder) = 
        Assert.True
            (String.IsNullOrWhiteSpace(result.Response.Content.ReadAsStringAsync().Result), 
             "Response should not contain body")
        result
    
    let ExpectSuccess(message : Response<'T>) = 
        if obj.ReferenceEquals(message.Error, null) = false then 
            Assert.True(1 = 2, "Expecting a success but received failure")
    
    let VerifyErrorCode (errorMessage : string) (message : Response<'T>) = 
        Assert.Equal<int>(errorMessage.GetErrorCode(), message.Error.ErrorCode)
    let VerifyHttpCode (statusCode) (handler : LoggingHandler) = 
        Assert.Equal<HttpStatusCode>(statusCode, handler.StatusCode())
