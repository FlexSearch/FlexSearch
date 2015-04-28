module IntegrationTests.Rest

open Autofac
open FlexSearch.Core
open Client
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
open Microsoft.Owin.Testing
open Swensen.Unquote

[<AutoOpenAttribute>]
module Helpers =
    open FlexSearch.Core
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    open System
    open System.Collections.Generic
    open System.IO
    open System.Linq
    open System.Net
    open System.Text
    open System.Threading
    
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
    
    let newIndex indexName = new Index.Dto(IndexName = indexName)
    
    let addField (index : Index.Dto) (fieldName : string) =
        index.Fields <- index.Fields |> Array.append [|new Field.Dto(fieldName)|]
    
//    // ----------------------------------------------------------------------------
//    // Global configuration
//    // ----------------------------------------------------------------------------
//    let url = "http://localhost:9800"
//    
//
//
//    /// <summary>
//    /// Represent a sample request
//    /// </summary>
//    type Example = 
//        { Resource : string
//          mutable Request : HttpWebRequest
//          mutable Response : HttpWebResponse
//          mutable ResponseBody : string
//          Id : string
//          Title : string
//          Method : string
//          Description : string
//          Uri : string
//          Querystring : string
//          Requestbody : string option
//          Output : ResizeArray<string>
//          OutputResponse : ResizeArray<string> }
//    
//    let example (id : string) (name : string) = 
//        let result = 
//            { Resource = ""
//              Id = id
//              Request = Unchecked.defaultof<_>
//              Response = Unchecked.defaultof<_>
//              ResponseBody = ""
//              Title = name
//              Method = ""
//              Description = ""
//              Uri = ""
//              Querystring = ""
//              Requestbody = None
//              Output = new ResizeArray<string>()
//              OutputResponse = new ResizeArray<string>() }
//        result.Output.Add(name)
//        result.Output.Add
//            ("''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''")
//        result.Output.Add("")
//        result
//    
//    let ofResource (name : string) (result : Example) = { result with Resource = name }
//    
//    let withDescription (desc : string) (result : Example) = 
//        result.Output.Add(desc)
//        result.Output.Add("")
//        result.Output.Add(sprintf ".. literalinclude:: example-%s.txt" result.Id)
//        result.Output.Add("\t:language: javascript")
//        result.Output.Add("")
//        { result with Description = desc }
//    
//    let request (meth : string) (uri : string) (result : Example) = 
//        { result with Method = meth
//                      Uri = uri }
//    
//    let withBody (body : string) (result : Example) = { result with Requestbody = Some(body) }
//


    /// <summary>
    /// Basic index configuration
    /// </summary>
    let mockIndexSettings() = 
        let index = new Index.Dto()
        index.IndexName <- "contact"
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Dto.Ram
        index.Fields <- 
         [| new Field.Dto("firstname", FieldType.Dto.Text)
            new Field.Dto("lastname", FieldType.Dto.Text)
            new Field.Dto("email", FieldType.Dto.ExactText)
            new Field.Dto("country", FieldType.Dto.Text)
            new Field.Dto("ipaddress", FieldType.Dto.ExactText)
            new Field.Dto("cvv2", FieldType.Dto.Int)
            new Field.Dto("description", FieldType.Dto.Highlight)
            new Field.Dto("fullname", FieldType.Dto.Text, ScriptName = "fullname") |]
        index.Scripts <- 
            [| new Script.Dto( ScriptName = "fullname", Source = """return fields.firstname + " " + fields.lastname;""", ScriptType = ScriptType.Dto.ComputedField) |]
        let searchProfileQuery = 
            new SearchQuery.Dto(index.IndexName, "firstname = '' AND lastname = '' AND cvv2 = '116' AND country = ''", 
                            QueryName = "test1")
        searchProfileQuery.MissingValueConfiguration.Add("firstname", MissingValueOption.ThrowError)
        searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
        searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
        index.SearchProfiles <- [| searchProfileQuery |]
        index
    
//    // ----------------------------------------------------------------------------
//    // Test assertions for Example based tests
//    // ----------------------------------------------------------------------------
//    let responseStatusEquals (status : HttpStatusCode) (result : Example) = 
//        status =? result.Response.StatusCode // "Status code does not match"
//        result
//    
//    let responseContainsHeader (header : string) (value : string) (result : Example) = 
//        value =? result.Response.Headers.Get(header) // "Header value does not match"
//        result
//    
//    let responseDataMatches (select : string) (expected : string) (result : Example) = 
//        let value = JObject.Parse(result.ResponseBody)
//        expected =? value.SelectToken("Data").SelectToken(select).ToString() // "Response does not match"
//        result
//
//    let responseErrorMatches (select : string) (expected : string) (result : Example) = 
//        let value = JObject.Parse(result.ResponseBody)
//        expected =? value.SelectToken("Error").SelectToken(select).ToString() // "Response does not match"
//        result
//    
//    let responseShouldContain (value : string) (result : Example) = 
//        result.ResponseBody.Contains(value) =? true // "Response does contain the required value"
//        result
//    
//    let responseContainsProperty (group : string) (key : string) (property : string) (expected : string) 
//        (result : Example) = 
//        let value = JObject.Parse(result.ResponseBody)
//        expected =? value.SelectToken(group).[key].[property].ToString() // "Response does contain the required property"
//        result
//    
//    let responseBodyIsNull (result : Example) = 
//        String.IsNullOrWhiteSpace(result.ResponseBody) =? true // "Response should not contain body"
//        result
//    

    // ----------------------------------------------------------------------------
    // Test assertions for FlexClient based tests
    // ----------------------------------------------------------------------------
    let hasHttpStatusCode expected response = response |> snd =? expected

    let hasErrorCode expected (response : Response<_> * HttpStatusCode) = (response |> fst).Error.ErrorCode =? expected

    let isSuccessful response = response |> snd =? HttpStatusCode.OK

    let isCreated response = response |> snd =? HttpStatusCode.Created

    let responseStatusEquals status result = result.Response.StatusCode =? status

    let data (response : Response<_> * HttpStatusCode) = (response |> fst).Data

//    // ----------------------------------------------------------------------------
//    // Test logic
//    // ----------------------------------------------------------------------------
//    let execute (result : Example) = 
//        result.OutputResponse.Add((sprintf "%s %s HTTP/1.1" result.Method result.Uri))
//        // Create & configure HTTP web request
//        let req = HttpWebRequest.Create(sprintf "%s%s" url result.Uri) :?> HttpWebRequest
//        req.ProtocolVersion <- HttpVersion.Version11
//        req.Method <- result.Method
//        // Encode body with POST data as array of bytes
//        if result.Requestbody.IsSome then 
//            let postBytes = Encoding.ASCII.GetBytes(result.Requestbody.Value)
//            req.ContentLength <- int64 postBytes.Length
//            // Write data to the request
//            let reqStream = req.GetRequestStream()
//            reqStream.Write(postBytes, 0, postBytes.Length)
//            reqStream.Close()
//        else req.ContentLength <- int64 0
//        let printHeaders (headerCollection : WebHeaderCollection) = 
//            for i = 0 to headerCollection.Count - 1 do
//                result.OutputResponse.Add(sprintf "%s:%s" headerCollection.Keys.[i] (headerCollection.GetValues(i).[0]))
//        
//        let print (resp : HttpWebResponse) = 
//            printHeaders (req.Headers)
//            if result.Requestbody.IsSome then 
//                let parsedJson = JsonConvert.DeserializeObject(result.Requestbody.Value)
//                result.OutputResponse.Add(JsonConvert.SerializeObject(parsedJson, Formatting.Indented))
//            result.OutputResponse.Add("")
//            result.OutputResponse.Add("")
//            result.OutputResponse.Add((sprintf "HTTP/1.1 %i %s" (int resp.StatusCode) (resp.StatusCode.ToString())))
//            printHeaders (resp.Headers)
//            if req.HaveResponse then 
//                let stream = resp.GetResponseStream()
//                let reader = new StreamReader(stream)
//                let responseBody = reader.ReadToEnd()
//                result.ResponseBody <- responseBody
//                let parsedJson = JsonConvert.DeserializeObject(responseBody)
//                if parsedJson <> Unchecked.defaultof<_> then 
//                    result.OutputResponse.Add("")
//                    result.OutputResponse.Add
//                        (sprintf "%s" (JsonConvert.SerializeObject(parsedJson, Formatting.Indented)))
//        
//        try 
//            result.Response <- req.GetResponse() :?> HttpWebResponse
//            print result.Response
//        with :? WebException as e -> 
//            result.Response <- e.Response :?> HttpWebResponse
//            print result.Response
//        for line in result.OutputResponse do
//            printfn "%s" line
//        result
//    
//    // ----------------------------------------------------------------------------
//    // Output logic
//    // ----------------------------------------------------------------------------
//    let document (result : Example) = 
//        if Directory.Exists(Constants.WebFolder) then
//            result.Output.Add(" ")
//            let path = Path.Combine(Constants.WebFolder, "example-" + result.Id + ".rst")
//            let examplePath = Path.Combine(Constants.WebFolder, "example-" + result.Id + ".txt") 
//            File.WriteAllLines(path, result.Output)
//            File.WriteAllLines(examplePath, result.OutputResponse)
//        result

type ``Index Creation Tests``() = 

    member __.``Accessing server root should return 200`` () = 
        owinServer
        |> request "GET" "/"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
    
    [<Example("post-indices-id-1", "Creating an index without any data")>]
    member __.``Creating an index without any parameters should return 200`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.AddIndex(newIndex indexName).Result
        actual |> isCreated
        client.DeleteIndex(indexName).Result |> isSuccessful
    
    [<Example("post-indices-id-2", "Duplicate index cannot be created")>]
    member __.``Duplicate index cannot be created`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        let actual = client.AddIndex(index).Result
        actual |> hasErrorCode "INDEX_ALREADY_EXISTS"
        actual |> hasHttpStatusCode HttpStatusCode.Conflict
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    member __.``Create response contains the id of the created index`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        let actual = client.AddIndex(index).Result
        actual |> isCreated
        (actual |> data).Id =? index.IndexName
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    member __.``Index cannot be created without IndexName`` (client : FlexClient, handler : LoggingHandler) = 
        let actual = client.AddIndex(newIndex "").Result
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest
        
    [<Example("post-indices-id-3", "")>]
    member __.``Create index with two field 'firstname' & 'lastname'`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let index = newIndex indexName
        index.Fields <- [| new Field.Dto("firstname"); new Field.Dto("lastname")|]
        client.AddIndex(index).Result |> isCreated
        client.DeleteIndex(indexName).Result |> isSuccessful
    
    [<Example("post-indices-id-4", "")>]
    member __.``Create an index with dynamic fields`` (client : FlexClient, handler : LoggingHandler) = 
        // The dynamic field are already constructed in the Index.Dto injected parameter
        // See fullname field
        let index = mockIndexSettings()
        client.AddIndex(index).Result |> isCreated
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("post-indices-id-5", "")>]
    member __.``Create an index by setting all properties`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        let actual = client.AddIndex(index).Result
        actual |> hasHttpStatusCode HttpStatusCode.Created
        client.DeleteIndex(index.IndexName).Result |> isSuccessful

type ``Index Update Tests``() = 
    [<Example("put-indices-id-1", "")>]
    member __.``Trying to update an index is not supported`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        let actual = client.UpdateIndex(index).Result
        actual |> hasErrorCode "HTTP_NOT_SUPPORTED"
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest

type ``Delete Index Tests``() = 
    [<Example("delete-indices-id-1", "")>]
    member __.``Delete an index by id`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        client.AddIndex(index).Result |> isCreated
        client.DeleteIndex(index.IndexName).Result |> isSuccessful
    
    [<Example("delete-indices-id-2", "")>]
    member __.``Trying to delete an non existing index will return error`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.DeleteIndex(indexName).Result
        actual |> hasErrorCode "INDEX_NOT_FOUND"
        actual |> hasHttpStatusCode HttpStatusCode.BadRequest

type ``Get Index Tests``() = 
    [<Example("get-indices-id-1", "")>]
    member __.``Getting an index detail by name`` (client : FlexClient, handler : LoggingHandler) = 
        client.AddIndex(mockIndexSettings()).Result |> isCreated
        let actual = client.GetIndex("contact").Result
        actual |> isSuccessful
        (actual |> data).IndexName =? "contact"
        actual |> hasHttpStatusCode HttpStatusCode.OK
    
    [<Example("get-indices-id-2", "")>]
    member __.``Getting an non existing index will return error`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
        let actual = client.GetIndex(indexName).Result
        actual |> hasErrorCode "INDEX_NOT_FOUND"
        actual |> hasHttpStatusCode HttpStatusCode.NotFound

//module ``Index Other Services Tests`` = 
//    [<Example("get-indices-id-status-1", "Get status of an index (offine)")>]
//    member __.``Newly created index is always offline`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
//        client.AddIndex(newIndex indexName).Result |> isSuccessful
//        let actual = client.GetIndexStatus(indexName).Result
//        actual |> isSuccessful
//        Assert.Equal<IndexState>(IndexState.Offline, actual.Data.Status)
//        client.DeleteIndex(indexName).Result |> isSuccessful
//    
//    [<Example("put-indices-id-status-1", "")>]
//    member __.``Set status of an index 'online'`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
//        client.AddIndex(newIndex indexName).Result |> isSuccessful
//        client.BringIndexOnline(indexName).Result |> isSuccessful
//        let actual = client.GetIndexStatus(indexName).Result
//        Assert.Equal<IndexState>(IndexState.Online, actual.Data.Status)
//        client.DeleteIndex(indexName).Result |> isSuccessful
//    
//    [<Example("put-indices-id-status-1", "")>]
//    member __.``Set status of an index 'offline'`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
//        client.AddIndex(new Index(IndexName = indexName, Online = true)).Result |> isSuccessful
//        let actual = client.GetIndexStatus(indexName).Result
//        Assert.Equal<IndexState>(IndexState.Online, actual.Data.Status)
//        client.SetIndexOffline(indexName).Result |> isSuccessful
//        let actual = client.GetIndexStatus(indexName).Result
//        Assert.Equal<IndexState>(IndexState.Offline, actual.Data.Status)
//        client.DeleteIndex(indexName).Result |> isSuccessful
//    
//    [<Example("get-indices-id-exists-1", "")>]
//    member __.``Check if a given index exists`` (client : FlexClient, indexName : Guid, handler : LoggingHandler) = 
//        let actual = client.IndexExists("country").Result
//        actual |> isSuccessful
//        Assert.Equal<bool>(true, actual.Data.Exists)
//    
//    [<Example("get-indices-1", "")>]
//    member __.``Get all indices`` (client : FlexClient, handler : LoggingHandler) = 
//        let actual = client.GetAllIndex().Result
//        // Should have at least country index
//        Assert.True(actual.Data.Count >= 1)