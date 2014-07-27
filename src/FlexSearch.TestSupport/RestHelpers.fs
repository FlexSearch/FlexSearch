namespace FlexSearch.TestSupport

open FlexSearch.Api
open FlexSearch.Api.Message
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
        | "DELETE" ->
            requestBuilder.Response <- requestBuilder.Server.HttpClient.DeleteAsync(requestBuilder.Uri).Result
        |_ -> failwithf "Not supported"
        requestBuilder
    
    let document (filename: string) (requestBuilder : RequestBuilder) = 
        let path = Path.Combine(DocumentationConf.DocumentationFolder, filename + ".adoc")
        let output = ResizeArray<string>()
        output.Add("""
[source,javascript]
----------------------------------------------------------------------------------
        """)
        // print request information
        output.Add(requestBuilder.RequestType + " " + requestBuilder.Uri)
        output.Add("")
        if String.IsNullOrWhiteSpace(requestBuilder.RequestBody) = false then 
            output.Add(requestBuilder.RequestBody)
            output.Add("")
        output.Add("")
        output.Add(sprintf "HTTP 1.1 %i %s" (int32(requestBuilder.Response.StatusCode)) (requestBuilder.Response.StatusCode.ToString()))
        for header in requestBuilder.Response.Headers do
            output.Add(header.Key + " : " + header.Value.First()) 
        
        let body = requestBuilder.Response.Content.ReadAsStringAsync().Result
        if String.IsNullOrWhiteSpace(body) <> true then
            let parsedJson = JsonConvert.DeserializeObject(body)
            if parsedJson <> Unchecked.defaultof<_> then 
                output.Add("")
                output.Add(sprintf "%s" (JsonConvert.SerializeObject(parsedJson, Formatting.Indented)))
        output.Add("----------------------------------------------------------------------------------")
        if Directory.Exists(DocumentationConf.DocumentationFolder) then 
            File.WriteAllLines(path, output)

    // ----------------------------------------------------------------------------
    // Test assertions
    // ----------------------------------------------------------------------------
    let responseStatusEquals (status : HttpStatusCode) (result : RequestBuilder) = 
        Assert.True(status.Equals(result.Response.StatusCode), ("Status code does not match: " + result.Response.StatusCode.ToString()))
        result
    
    let responseContainsHeader (header : string) (value : string) (result : RequestBuilder) = 
        Assert.Equal<string>(value, result.Response.Headers.First(fun x -> x.Key = header).Value.First()) // "Header value does not match"
        result
        
    let responseMatches (select : string) (expected : string) (result : RequestBuilder) = 
        let value = JObject.Parse(result.Response.Content.ReadAsStringAsync().Result)
        Assert.Equal<string>(expected, value.SelectToken(select).ToString()) // "Response does not match"
        result
    
        
    let responseShouldContain (value : string) (result : RequestBuilder) = 
        Assert.True(result.Response.Content.ReadAsStringAsync().Result.Contains(value), "Response does contain the required value")
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
    

