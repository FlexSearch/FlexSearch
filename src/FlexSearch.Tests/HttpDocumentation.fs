module HttpDocumentation

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
open HttpClient
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

let url = "http://localhost:9800"
let rootFolder = @"E:\Python27\Scripts\pelican\Scripts\OneDrive\Sites\documentation\content\requests"

let request uri httpMethod (body : string option) = 
    let result = new ResizeArray<string>()
    result.Add((sprintf "%s %s HTTP/1.1" httpMethod uri))
    // Create & configure HTTP web request
    let req = HttpWebRequest.Create(sprintf "%s%s" url uri) :?> HttpWebRequest
    req.ProtocolVersion <- HttpVersion.Version11
    req.Method <- httpMethod
    // Encode body with POST data as array of bytes
    if body.IsSome then 
        let postBytes = Encoding.ASCII.GetBytes(body.Value)
        req.ContentLength <- int64 postBytes.Length
        // Write data to the request
        let reqStream = req.GetRequestStream()
        reqStream.Write(postBytes, 0, postBytes.Length)
        reqStream.Close()
    else req.ContentLength <- int64 0
    let printHeaders (headerCollection : WebHeaderCollection) = 
        for i = 0 to headerCollection.Count - 1 do
            result.Add(sprintf "%s:%s" headerCollection.Keys.[i] (headerCollection.GetValues(i).[0]))
    
    let print (resp : HttpWebResponse) = 
        printHeaders (req.Headers)
        if body.IsSome then 
            let parsedJson = JsonConvert.DeserializeObject(body.Value)
            result.Add(JsonConvert.SerializeObject(parsedJson, Formatting.Indented))
        result.Add("")
        result.Add("")
        result.Add((sprintf "HTTP/1.1 %i %s" (int resp.StatusCode) (resp.StatusCode.ToString())))
        printHeaders (resp.Headers)
        if req.HaveResponse then 
            let stream = resp.GetResponseStream()
            let reader = new StreamReader(stream)
            let responseBody = reader.ReadToEnd()
            let parsedJson = JsonConvert.DeserializeObject(responseBody)
            if parsedJson <> Unchecked.defaultof<_> then 
                result.Add("")
                result.Add(JsonConvert.SerializeObject(parsedJson, Formatting.Indented))
    
    try 
        print (req.GetResponse() :?> HttpWebResponse)
    with :? WebException as e -> print (e.Response :?> HttpWebResponse)
    result

let document (result : ResizeArray<string>) (i : int) = 
    let path = Path.Combine(rootFolder, i.ToString() + ".txt")
    File.WriteAllLines(path, result)

let generateDocumentation() = 
    let serverSettings = GetServerSettings(ConfFolder.Value + "\\Config.json")
    let node = new NodeService(serverSettings, true)
    node.Start()
    let mutable response = Unchecked.defaultof<_>
    // Example 1: Create index without any field
    1 |> document (request "/test" "GET" None)
    ()
