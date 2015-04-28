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

//
//    /// <summary>
//    /// Basic index configuration
//    /// </summary>
//    let mockIndexSettings() = 
//        let index = new Index.Dto()
//        index.IndexName <- "contact"
//        index.Online <- true
//        index.IndexConfiguration.DirectoryType <- DirectoryType.Dto.Ram
//        index.Fields <- 
//         [| new Field.Dto("firstname", FieldType.Dto.Text)
//            new Field.Dto("lastname", FieldType.Dto.Text)
//            new Field.Dto("email", FieldType.Dto.ExactText)
//            new Field.Dto("country", FieldType.Dto.Text)
//            new Field.Dto("ipaddress", FieldType.Dto.ExactText)
//            new Field.Dto("cvv2", FieldType.Dto.Int)
//            new Field.Dto("description", FieldType.Dto.Highlight)
//            new Field.Dto("fullname", FieldType.Dto.Text, ScriptName = "fullname") |]
//        index.Scripts <- 
//            [| new Script.Dto( ScriptName = "fullname", Source = """return fields.firstname + " " + fields.lastname;""", ScriptType = ScriptType.Dto.ComputedField) |]
//        let searchProfileQuery = 
//            new SearchQuery.Dto(index.IndexName, "firstname = '' AND lastname = '' AND cvv2 = '116' AND country = ''", 
//                            QueryName = "test1")
//        searchProfileQuery.MissingValueConfiguration.Add("firstname", MissingValueOption.ThrowError)
//        searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
//        searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
//        index.SearchProfiles <- [| searchProfileQuery |]
//        index
//    
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
    member __.``Create an index with dynamic fields`` (client : FlexClient, index : Index.Dto, handler : LoggingHandler) = 
        // The dynamic field are already constructed in the Index.Dto injected parameter
        // See fullname field
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

//module ``Delete Index`` = 
//    [<Example("delete-indices-id-1", "")>]
//    member __.``Delete an index by id`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
//        client.AddIndex(newIndex indexName).Result |> isSuccessful
//        let actual = client.DeleteIndex(indexName).Result
//        actual |> isSuccessful
//        actual |> hasHttpStatusCode HttpStatusCode.OK
//    
//    [<Example("delete-indices-id-2", "")>]
//    member __.``Trying to delete an non existing index will return error`` (client : FlexClient, indexName : string, 
//                                                                      handler : LoggingHandler) = 
//        let actual = client.DeleteIndex(indexName).Result
//        actual |> VerifyErrorCode Errors.INDEX_NOT_FOUND
//        actual |> hasHttpStatusCode HttpStatusCode.NotFound
//
//module ``Get Index Tests`` = 
//    [<Example("get-indices-id-1", "")>]
//    member __.``Getting an index detail by name`` (client : FlexClient, indexName : string, handler : LoggingHandler) = 
//        let actual = client.GetIndex("country").Result
//        actual |> isSuccessful
//        Assert.Equal<string>("country", actual.Data.IndexName)
//        actual |> hasHttpStatusCode HttpStatusCode.OK
//    
//    [<Example("delete-indices-id-2", "")>]
//    member __.``Getting an non existing index will return error`` (client : FlexClient, indexName : string, 
//                                                             handler : LoggingHandler) = 
//        let actual = client.DeleteIndex(indexName).Result
//        actual |> VerifyErrorCode Errors.INDEX_NOT_FOUND
//        actual |> hasHttpStatusCode HttpStatusCode.NotFound
//
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





//module WebserviceTests
// 
//    open FlexSearch.Core
//    open Newtonsoft.Json
//    open Newtonsoft.Json.Linq
//    open System
//    open System.Collections.Generic
//    open System.IO
//    open System.Linq
//    open System.Net
//    open System.Text
//    open System.Threading
//    open Swensen.Unquote
//    // ----------------------------------------------------------------------------
//    // Global configuration
//    // ----------------------------------------------------------------------------
//    let url = "http://localhost:9800"
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
//    /// <summary>
//    /// Basic index configuration
//    /// </summary>
//    let mockIndexSettings() = 
//        let index = new Index.Dto()
//        index.IndexName <- "contact"
//        index.Online <- true
//        index.IndexConfiguration.DirectoryType <- DirectoryType.Dto.Ram
//        index.Fields <- 
//         [| new Field.Dto("firstname", FieldType.Dto.Text)
//            new Field.Dto("lastname", FieldType.Dto.Text)
//            new Field.Dto("email", FieldType.Dto.ExactText)
//            new Field.Dto("country", FieldType.Dto.Text)
//            new Field.Dto("ipaddress", FieldType.Dto.ExactText)
//            new Field.Dto("cvv2", FieldType.Dto.Int)
//            new Field.Dto("description", FieldType.Dto.Highlight)
//            new Field.Dto("fullname", FieldType.Dto.Text, ScriptName = "fullname") |]
//        index.Scripts <- 
//            [| new Script.Dto( ScriptName = "fullname", Source = """return fields.firstname + " " + fields.lastname;""", ScriptType = ScriptType.Dto.ComputedField) |]
//        let searchProfileQuery = 
//            new SearchQuery.Dto(index.IndexName, "firstname = '' AND lastname = '' AND cvv2 = '116' AND country = ''", 
//                            QueryName = "test1")
//        searchProfileQuery.MissingValueConfiguration.Add("firstname", MissingValueOption.ThrowError)
//        searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
//        searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
//        index.SearchProfiles <- [| searchProfileQuery |]
//        index
//    
//    // ----------------------------------------------------------------------------
//    // Test assertions
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
//    
//    type ``REST Service Tests``() = 
//        do ensureServerIsRunning owinServer
//        
//        //[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.``Index creation test 1``() = 
//            let indexName = Guid.NewGuid().ToString("N")
//            example "post-index-1" "Create index without any field"
//            |> ofResource "Index"
//            |> withDescription """
//The newly created index will be offline as the Online parameter is set to false as default. An index has to be opened after creation to enable indexing.
//        """
//            |> request "POST" "/indices"
//            |> withBody ("""
//            {
//                "IndexName" : """ + indexName + """
//            }
//            """)
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.``Index creation test 2``() = 
//            let indexName = Guid.NewGuid().ToString("N")
//            example "post-index-1" "Create index without any field"
//            |> ofResource "Index"
//            |> withDescription """
//The newly created index will be offline as the Online parameter is set to false as default. An index has to be opened after creation to enable indexing.
//        """
//            |> request "POST" ("/indices/" + indexName)
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> ignore
//            example "post-index-2" "Duplicate index cannot be created."
//            |> ofResource "Index"
//            |> withDescription """
//The newly created index will be offline as the Online parameter is set to false as default. An index has to be opened after creation to enable indexing.
//        """
//            |> request "POST" ("/indices/" + indexName)
//            |> execute
//            |> responseStatusEquals HttpStatusCode.BadRequest
//            |> responseErrorMatches "ErrorCode" "1002"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.``Index creation test 3``() = 
//            example "post-index-3" "Create index with two field 'firstname' & 'lastname'"
//            |> ofResource "Index"
//            |> withDescription """
//All field names should be lower case and should not contain any spaces. This is to avoid case based mismatching on field names. Fields have many 
//other configurable properties but Field Type is the only mandatory parameter. Refer to Index Field for more information about field properties.
//        """
//            |> request "POST" ("/indices/" + Guid.NewGuid().ToString("N"))
//            |> withBody """
//            {
//                "Fields" : {
//                    "firstname" : { FieldType : "Text" },
//                    "lastname" : { FieldType : "Text" }
//                }
//            }
//            """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.``Create update and delete an index``() = 
//            let indexName = Guid.NewGuid().ToString("N")
//            example "post-index-4" "Create index with computed field"
//            |> ofResource "Index"
//            |> withDescription """
//Fields can be dynamic in nature and can be computed at index time from the passed data. Computed field requires custom scripts which defines the 
//field data creation logic. Let’s create an index field called fullname which is a concatenation of ‘firstname’ and ‘lastname’.
//
//Computed fields requires ScriptName property to be set in order load a custom script. FlexSearch scripts are 
//dynamically compiled to .net dlls so performance wise they are similar to native .net code. Scripts are written 
//in C#. But it would be difficult to write complex scripts in single line to pass to the Script source, that 
//is why Flex supports Multi-line and File based scripts. Refer to Script for more information about scripts.
//        """
//            |> request "POST" ("/indices/" + indexName)
//            |> withBody """
//            {
//                "Fields" : {
//                    "firstname" : { FieldType : "Text" },
//                    "lastname" : { FieldType : "Text" },
//                    "fullname" : {FieldType : "Text", ScriptName : "fullnamescript"}
//                },
//                "Scripts" : {
//                    fullnamescript : {
//                        ScriptType : "ComputedField",
//                        Source : "return fields[\"firstname\"] + \" \" + fields[\"lastname\"];"
//                    }
//                }
//            }
//            """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//            |> ignore
//            example "put-index-1" "Updating an existing index"
//            |> ofResource "Index"
//            |> withDescription """
//There are a number of parameters which can be set for a given index. For more information about each parameter please refer to Glossary.
//        """
//            |> request "PUT" ("/indices/" + indexName)
//            |> withBody """
//        {
//            "Fields" : {
//                "firstname" : { FieldType : "Text" },
//                "lastname" : { FieldType : "Text" },
//                "fullname" : {FieldType : "Text", ScriptName : "fullnamescript"},
//                "desc" : { FieldType : "Stored" },
//            },
//            "Scripts" : {
//                fullnamescript : {
//                    ScriptType : "ComputedField",
//                    Source : "return fields[\"firstname\"] + \" \" + fields[\"lastname\"];"
//                }
//            }
//        }
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//            |> ignore
//            example "delete-index-1" "Deleting an existing index"
//            |> ofResource "Index"
//            |> withDescription ""
//            |> request "DELETE" ("/indices/" + indexName)
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.IndexCreationTest5() = 
//            example "post-index-5" "Create index by setting all properties"
//            |> ofResource "Index"
//            |> withDescription """
//There are a number of parameters which can be set for a given index. For more information about each parameter please refer to Glossary.
//        """
//            |> request "POST" ("/indices/" + Guid.NewGuid().ToString("N"))
//            |> withBody (JsonConvert.SerializeObject(mockIndexSettings()))
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//        
//// Index update is not supported anymore
////        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
////        member __.IndexUpdateTest2() = 
////            example "put-index-2" "Index update request with wrong index name returns error"
////            |> ofResource "Index"
////            |> withDescription ""
////            |> request "PUT" "/indices/indexdoesnotexist"
////            |> withBody """
////        {
////            "Fields" : {
////                "firstname" : { FieldType : "Text" },
////                "lastname" : { FieldType : "Text" },
////                "fullname" : {FieldType : "Text", ScriptName : "fullnamescript"},
////                "desc" : { FieldType : "Stored" },
////            },
////            "Scripts" : {
////                fullnamescript : {
////                    ScriptType : "ComputedField",
////                    Source : "return fields[\"firstname\"] + \" \" + fields[\"lastname\"];"
////                }
////            }
////        }
////        """
////            |> execute
////            |> responseStatusEquals HttpStatusCode.BadRequest
////            |> responseMatches "ErrorCode" "1000"
////            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.IndexDeleteTest2() = 
//            example "delete-index-2" "Deleting an non-existing index will return an error"
//            |> ofResource "Index"
//            |> withDescription ""
//            |> request "DELETE" "/indices/indexDoesNotExist"
//            |> execute
//            |> responseStatusEquals HttpStatusCode.BadRequest
//            |> responseErrorMatches "ErrorCode" "INDEX_NOT_FOUND"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.IndexGetTest1() = 
//            example "get-index-1" "Getting an index detail by name"
//            |> ofResource "Index"
//            |> withDescription ""
//            |> request "GET" "/indices/contact"
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseDataMatches "IndexName" "contact"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.IndexGetTest2() = 
//            example "get-index-2" "Getting an index detail by name (non existing index)"
//            |> ofResource "Index"
//            |> withDescription ""
//            |> request "GET" "/indices/indexDoesNotExist"
//            |> execute
//            |> responseStatusEquals HttpStatusCode.BadRequest
//            |> responseErrorMatches "ErrorCode" "INDEX_NOT_FOUND"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.IndexExistsTest1() = 
//            example "get-index-exists-1" "Checking if an index exists (true case)"
//            |> ofResource "Exists"
//            |> withDescription ""
//            |> request "GET" "/indices/contact/exists"
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.IndexExistsTest2() = 
//            example "get-index-exists-2" "Checking if an index exists (false case)"
//            |> ofResource "Exists"
//            |> withDescription ""
//            |> request "GET" "/indices/indexDoesNotExist/exists"
//            |> execute
//            |> responseStatusEquals HttpStatusCode.BadRequest
//            |> responseErrorMatches "ErrorCode" "1000"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.IndexStatusTest() = 
//            let indexName = Guid.NewGuid().ToString("N")
//            example "" ""
//            |> request "POST" ("/indices/" + indexName)
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> ignore
//            example "get-index-status-2" "Getting status of an index (offline)"
//            |> ofResource "Status"
//            |> withDescription ""
//            |> request "GET" ("/indices/" + indexName + "/status")
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseDataMatches "Status" "Offline"
//            |> document
//            |> ignore
//            example "post-index-status-1" "Setting status of an index to on-line"
//            |> ofResource "Status"
//            |> withDescription ""
//            |> request "POST" ("/indices/" + indexName + "/status/online")
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//            |> ignore
//            example "get-index-status-1" "Getting status of an index"
//            |> ofResource "Status"
//            |> withDescription ""
//            |> request "GET" ("/indices/" + indexName + "/status")
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseDataMatches "Status" "Online"
//            |> document
//            |> ignore
//            example "post-index-status-2" "Setting status of an index to off-line"
//            |> ofResource "Status"
//            |> withDescription ""
//            |> request "POST" ("/indices/" + indexName + "/status/offline")
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.IndexDocumentsTest() = 
//            let indexName = Guid.NewGuid().ToString("N")
//            example "" ""
//            |> request "POST" ("/indices/" + indexName)
//            |> withBody """
//            {
//                "Online": true,
//                "Fields" : {
//                    "firstname" : { FieldType : "Text" },
//                    "lastname" : { FieldType : "Text" }
//                }
//            }
//            """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> ignore
//            example "post-index-document-id-1" "Add a document to an index"
//            |> ofResource "Documents"
//            |> withDescription ""
//            |> request "POST" ("/indices/" + indexName + "/documents/51")
//            |> withBody """
//            {
//                "firstname" : "Seemant",
//                "lastname" : "Rajvanshi"
//            }
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//            |> ignore
//            Thread.Sleep(5000)
//            example "get-index-document-id-1" "Get a document by an id from an index"
//            |> ofResource "Documents"
//            |> withDescription ""
//            |> request "GET" ("/indices/" + indexName + "/documents/51")
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseDataMatches Constants.IdField "51"
//            |> document
//            |> ignore
//            example "put-index-document-id-1" "Update a document by id to an index"
//            |> ofResource "Documents"
//            |> withDescription ""
//            |> request "PUT" ("/indices/" + indexName + "/documents/51")
//            |> withBody """
//            {
//                "firstname" : "Seemant",
//                "lastname" : "Rajvanshi"
//            }
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//            |> ignore
//            example "delete-index-document-id-1" "Delete a document by id from an index"
//            |> ofResource "Documents"
//            |> withDescription ""
//            |> request "DELETE" ("/indices/" + indexName + "/documents/51")
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseBodyIsNull
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.IndexDocumentsTest5() = 
//            example "get-index-document-1" "Get top 10 documents from an index"
//            |> ofResource "Documents"
//            |> withDescription ""
//            |> request "GET" "/indices/contact/documents"
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            //|> responseContainsHeader "RecordsReturned" "10"
//            //|> responseContainsHeader "TotalAvailable" "50"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchTermQueryTest1() = 
//            example "post-index-search-termquery-1" "Term search using ``=`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to match all documents where firstname = 'Kathy' and lastname = 'Banks'
//
//::
//
//    firstname = 'Kathy' and lastname = 'Banks'
//
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "firstname = 'Kathy' and lastname = 'Banks'"
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseDataMatches "RecordsReturned" "1"
//            |> responseDataMatches "TotalAvailable" "1"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchTermQueryTest2() = 
//            example "post-index-search-termquery-2" "Term search using ``eq`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to match all documents where firstname eq 'Kathy' and lastname eq 'Banks'
//
//::
//
//    firstname eq 'Kathy' and lastname eq 'Banks'
//
//        """
//            |> request "POST" "/indices/contact/search?c=*"
//            |> withBody """
//        {
//          "QueryString": "firstname eq 'Kathy' and lastname eq 'Banks'",
//          "ReturnFlatResult": true
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "1"
//            |> responseContainsHeader "TotalAvailable" "1"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchFuzzyQueryTest1() = 
//            example "post-index-search-fuzzyquery-1" "Fuzzy search using ``fuzzy`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to fuzzy match all documents where firstname is 'Kathy'
//
//::
//
//    firstname fuzzy 'Kathy'
//
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "firstname fuzzy 'Kathy'"
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseDataMatches "RecordsReturned" "3"
//            |> responseDataMatches "TotalAvailable" "3"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchFuzzyQueryTest2() = 
//            example "post-index-search-fuzzyquery-2" "Fuzzy search using ``~=`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to fuzzy match all documents where firstname is 'Kathy'
//
//::
//
//    firstname ~= 'Kathy'
//
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "firstname ~= 'Kathy'",
//          "ReturnFlatResult": true
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "3"
//            |> responseContainsHeader "TotalAvailable" "3"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchFuzzyQueryTest3() = 
//            example "post-index-search-fuzzyquery-3" "Fuzzy search using slop parameter"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to fuzzy match all documents where firstname is 'Kathy' and slop is 2
//
//::
//    
//    firstname ~= 'Kathy'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "firstname ~= 'Kathy' {slop : '2'}",
//          "ReturnFlatResult": true
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "3"
//            |> responseContainsHeader "TotalAvailable" "3"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchPhraseQueryTest1() = 
//            example "post-index-search-phrasequery-1" "Phrase search using ``match`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to fuzzy match all documents where description is 'Nunc purus'
//
//::
//
//    description match 'Nunc purus'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "description match 'Nunc purus'",
//          "ReturnFlatResult": true
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "4"
//            |> responseContainsHeader "TotalAvailable" "4"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchWildCardQueryTest1() = 
//            example "post-index-search-wildcardquery-1" "Wildcard search using ``like`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to fuzzy match all documents where firstname is like 'Ca*'
//
//::
//
//    firstname like 'Ca*'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "firstname like 'ca*'",
//          "ReturnFlatResult": true
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "3"
//            |> responseContainsHeader "TotalAvailable" "3"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchWildCardQueryTest2() = 
//            example "post-index-search-wildcardquery-2" "Wildcard search using ``%=`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to fuzzy match all documents where firstname is like 'Ca*'
//
//::
//
//    firstname %= 'Ca*'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "firstname %= 'Ca*'",
//          "ReturnFlatResult": true
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "3"
//            |> responseContainsHeader "TotalAvailable" "3"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchWildCardQueryTest3() = 
//            example "post-index-search-wildcardquery-3" "Wildcard search using ``%=`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to fuzzy match all documents where firstname is like 'Cat?y'. This can
//be used to match one character.
//
//::
//    
//    firstname %= 'Cat?y'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "firstname %= 'Cat?y'",
//          "ReturnFlatResult": true
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "1"
//            |> responseContainsHeader "TotalAvailable" "1"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchRegexQueryTest1() = 
//            example "post-index-search-regexquery-1" "Regex search using ``regex`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to fuzzy match all documents where firstname is like '[ck]athy'. This can
//be used to match one character.
//
//::
//    
//    firstname regex '[ck]Athy'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "firstname regex '[ck]Athy'",
//          "ReturnFlatResult": true
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "3"
//            |> responseContainsHeader "TotalAvailable" "3"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchMatchallQueryTest1() = 
//            example "post-index-search-matchallquery-1" "Match all search using ``matchall`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to to match all documents in the index.
//
//::
//    
//    firstname matchall '*'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "firstname matchall '*'",
//          "ReturnFlatResult": true,
//          Count: 1
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "1"
//            |> responseContainsHeader "TotalAvailable" "50"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchNumericRangeQueryTest1() = 
//            example "post-index-search-numericrangequery-1" "Range search using ``>`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to to match all documents with cvv2 greater than 100 in the index.
//    
//::
//
//    cvv2 > '100'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "cvv2 > '100'",
//          "ReturnFlatResult": true,
//          Count: 1
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "1"
//            |> responseContainsHeader "TotalAvailable" "48"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchNumericRangeQueryTest2() = 
//            example "post-index-search-numericrangequery-2" "Range search using ``>=`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to to match all documents with cvv2 greater than or equal to 200 in the index.
//    
//::
//    
//    cvv2 >= '200'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "cvv2 >= '200'",
//          "ReturnFlatResult": true,
//          Count: 1
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "1"
//            |> responseContainsHeader "TotalAvailable" "41"
//            |> document
//        
//        ////[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchNumericRangeQueryTest3() = 
//            example "post-index-search-numericrangequery-3" "Range search using ``<`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to to match all documents with cvv2 less than 150 in the index.
//
//::
//    
//    cvv2 < '150'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "cvv2 < '150'",
//          "ReturnFlatResult": true,
//          Count: 1
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "1"
//            |> responseContainsHeader "TotalAvailable" "7"
//            |> document
//        
//        //[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchNumericRangeQueryTest4() = 
//            example "post-index-search-numericrangequery-4" "Range search using ``<=`` operator"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to to match all documents with cvv2 less than or equal to 500 in the index.
//
//::
//    
//    cvv2 <= '500'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname"
//            |> withBody """
//        {
//          "QueryString": "cvv2 <= '500'",
//          "ReturnFlatResult": true,
//          Count: 1
//        }    
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> responseContainsHeader "RecordsReturned" "1"
//            |> responseContainsHeader "TotalAvailable" "26"
//            |> document
//        
//        //[<Fact>][<TraitAttribute("Category", "Rest")>]
//        member __.SearchHighlightFeatureTest1() = 
//            let query = new SearchQuery.Dto("contact", " description = 'Nullam'")
//            let highlight = new List<string>()
//            highlight.Add("description")
//            query.Highlights <- new HighlightOption.Dto(highlight.ToArray())
//            example "post-index-search-highlightfeature-1" "Text highlighting basic example"
//            |> ofResource "Search"
//            |> withDescription """
//The below is the query to highlight 'Nullam' is description field.
//
//::
//    description = 'Nullam'
//        """
//            |> request "POST" "/indices/contact/search?c=firstname,lastname,description"
//            |> withBody """
//        {
//          "Count": 2,  
//          "Highlights": {
//            "FragmentsToReturn": 2,
//            "HighlightedFields": [
//              "description"
//            ],
//            "PostTag": "</B>",
//            "PreTag": "</B>"
//          },
//          "QueryString": " description = 'Nullam'",
//          }   
//        """
//            |> execute
//            |> responseStatusEquals HttpStatusCode.OK
//            |> document
