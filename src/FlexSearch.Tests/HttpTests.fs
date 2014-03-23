module HttpTests

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

// ----------------------------------------------------------------------------
// Test server initialization
// ----------------------------------------------------------------------------
let mutable serverRunning = false
let initializeServer() =
    if serverRunning <> true then
        let serverSettings = GetServerSettings(ConfFolder.Value + "\\Config.json")
        let node = new NodeService(serverSettings, true)
        node.Start()
        serverRunning <- true

// ----------------------------------------------------------------------------
// Global configuration
// ----------------------------------------------------------------------------
let url = "http://localhost:9800"

/// <summary>
/// Represent a sample request
/// </summary>
type Example = 
    { Resource : string
      mutable Request : HttpWebRequest
      mutable Response : HttpWebResponse
      mutable ResponseBody : string
      Id : string
      Title : string
      Method : string
      Description : string
      Uri : string
      Querystring : string
      Requestbody : string option
      TestCases : ResizeArray<Test>
      Output : ResizeArray<string> }

let example (id : string) (name : string) = 
    let result = 
        { Resource = ""
          Id = id
          Request = Unchecked.defaultof<_>
          Response = Unchecked.defaultof<_>
          ResponseBody = ""
          Title = name
          Method = ""
          Description = ""
          Uri = ""
          Querystring = ""
          Requestbody = None
          TestCases = new ResizeArray<Test>()
          Output = new ResizeArray<string>() }
    result.Output.Add("Title: " + name)
    result.Output.Add("Category: Examples")
    result.Output.Add("Method: Example")
    result.Output.Add("Uri: " + name)
    result.Output.Add("Slug: " + id)
    result.Output.Add("Date: 2010-12-03 10:20")
    initializeServer()
    result

let ofResource (name : string) (result : Example) = { result with Resource = name }

let withDescription (desc : string) (result : Example) = 
    result.Output.Add("<!--- start -->")
    result.Output.Add(desc)
    result.Output.Add("```javascript")
    { result with Description = desc }

let request (meth : string) (uri : string) (result : Example) = 
    { result with Method = meth
                  Uri = uri }

let withBody (body : string) (result : Example) = { result with Requestbody = Some(body) }

// ----------------------------------------------------------------------------
// Test assertions
// ----------------------------------------------------------------------------
let responseStatusEquals (status : HttpStatusCode) (result : Example) = 
    let test = 
        testCase (sprintf "%s: should return %s" result.Title (status.ToString())) 
        <| fun _ -> result.Response.StatusCode |> should equal status
    result.TestCases.Add(test)
    result

let responseContainsHeader (header : string) (value : string) (result : Example) = 
    let test = testCase "Should contain header" <| fun _ -> result.Response.Headers.Get(header) |> should equal value
    result.TestCases.Add(test)
    result

let responseMatches (select : string) (expected : string) (result : Example) = 
    let test = 
        testCase (sprintf "%s: response should match %s" result.Title expected) <| fun _ -> 
            let value = JObject.Parse(result.ResponseBody)
            value.SelectToken(select).ToString() |> should equal expected
    result.TestCases.Add(test)
    result

let responseShouldContain (value : string) (result : Example) = 
    let test = 
        testCase (sprintf "%s: response should contain %s" result.Title value) 
        <| fun _ -> result.ResponseBody.Contains(value) |> should equal true
    result.TestCases.Add(test)
    result

let responseContainsProperty (group : string) (key : string) (property : string) (expected : string) (result : Example) = 
    let test = 
        testCase (sprintf "%s: should contain property %s" result.Title property) <| fun _ -> 
            let value = JObject.Parse(result.ResponseBody)
            value.SelectToken(group).[key].[property].ToString() |> should equal expected
    result.TestCases.Add(test)
    result

let responseBodyIsNull (result : Example) = 
    let test = 
        testCase (sprintf "%s: response should not contain body" result.Title) 
        <| fun _ -> String.IsNullOrWhiteSpace(result.ResponseBody) |> should equal true
    result.TestCases.Add(test)
    result

let runAssertions  (result : Example) = 
    testList result.Title result.TestCases

// ----------------------------------------------------------------------------
// Test logic
// ----------------------------------------------------------------------------
let execute (result : Example) = 
    result.Output.Add((sprintf "%s %s HTTP/1.1" result.Method result.Uri))
    // Create & configure HTTP web request
    let req = HttpWebRequest.Create(sprintf "%s%s" url result.Uri) :?> HttpWebRequest
    req.ProtocolVersion <- HttpVersion.Version11
    req.Method <- result.Method
    // Encode body with POST data as array of bytes
    if result.Requestbody.IsSome then 
        let postBytes = Encoding.ASCII.GetBytes(result.Requestbody.Value)
        req.ContentLength <- int64 postBytes.Length
        // Write data to the request
        let reqStream = req.GetRequestStream()
        reqStream.Write(postBytes, 0, postBytes.Length)
        reqStream.Close()
    else req.ContentLength <- int64 0
    let printHeaders (headerCollection : WebHeaderCollection) = 
        for i = 0 to headerCollection.Count - 1 do
            result.Output.Add(sprintf "%s:%s" headerCollection.Keys.[i] (headerCollection.GetValues(i).[0]))
    
    let print (resp : HttpWebResponse) = 
        printHeaders (req.Headers)
        if result.Requestbody.IsSome then 
            let parsedJson = JsonConvert.DeserializeObject(result.Requestbody.Value)
            result.Output.Add(JsonConvert.SerializeObject(parsedJson, Formatting.Indented))
        result.Output.Add("")
        result.Output.Add("")
        result.Output.Add((sprintf "HTTP/1.1 %i %s" (int resp.StatusCode) (resp.StatusCode.ToString())))
        printHeaders (resp.Headers)
        if req.HaveResponse then 
            let stream = resp.GetResponseStream()
            let reader = new StreamReader(stream)
            let responseBody = reader.ReadToEnd()
            result.ResponseBody <- responseBody
            let parsedJson = JsonConvert.DeserializeObject(responseBody)
            if parsedJson <> Unchecked.defaultof<_> then 
                result.Output.Add("")
                result.Output.Add(JsonConvert.SerializeObject(parsedJson, Formatting.Indented))
    
    try 
        result.Response <- req.GetResponse() :?> HttpWebResponse
        print result.Response
    with :? WebException as e -> 
        result.Response <- e.Response :?> HttpWebResponse
        print result.Response
    result

// ----------------------------------------------------------------------------
// Output logic
// ----------------------------------------------------------------------------
let document (result : Example) = 
    result.Output.Add("```")
    let path = Path.Combine(Helpers.DocumentationConf.DocumentationFolder, "requests", result.Id + ".md")
    if Directory.Exists(Helpers.DocumentationConf.DocumentationFolder) then File.WriteAllLines(path, result.Output)
    result

// ----------------------------------------------------------------------------
// Tests
// ----------------------------------------------------------------------------
[<Tests>]
let IndexCreationTest1() = 
    example "post-index-1" "Create index without any field"
    |> ofResource "Index"
    |> withDescription """
The newly created index will be offline as the Online parameter is set to false as default. An index has to be opened after creation to enable indexing.
"""
    |> request "POST" "/test1"
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseBodyIsNull
    |> document
    |> runAssertions

[<Tests>]
let IndexCreationTest2() = 
    example "post-index-2" "Duplicate index cannot be created."
    |> ofResource "Index"
    |> withDescription """
The newly created index will be offline as the Online parameter is set to false as default. An index has to be opened after creation to enable indexing.
"""
    |> request "POST" "/test1"
    |> execute
    |> responseStatusEquals HttpStatusCode.BadRequest
    |> responseMatches "ErrorCode" "1002"
    |> document
    |> runAssertions

[<Tests>]
let IndexCreationTest3() = 
    example "post-index-3" "Create index with two field 'firstname' & 'lastname'"
    |> ofResource "Index"
    |> withDescription """
All field names should be lower case and should not contain any spaces. This is to avoid case based mismatching on field names. Fields have many 
other configurable properties but Field Type is the only mandatory parameter. Refer to Index Field for more information about field properties.
"""
    |> request "POST" "/test2"
    |> withBody """
    {
        "Fields" : {
            "firstname" : { FieldType : "Text" },
            "lastname" : { FieldType : "Text" }
        }
    }
    """
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseBodyIsNull
    |> document
    |> runAssertions

[<Tests>]
let IndexCreationTest4() = 
    example "post-index-4" "Create index with computed field"
    |> ofResource "Index"
    |> withDescription """
Fields can be dynamic in nature and can be computed at index time from the passed data. Computed field requires custom scripts which defines the 
field data creation logic. Let’s create an index field called fullname which is a concatenation of ‘firstname’ and ‘lastname’.

Computed fields requires ScriptName property to be set in order load a custom script. FlexSearch scripts are 
dynamically compiled to .net dlls so performance wise they are similar to native .net code. Scripts are written 
in C#. But it would be difficult to write complex scripts in single line to pass to the Script source, that 
is why Flex supports Multi-line and File based scripts. Refer to Script for more information about scripts.
"""
    |> request "POST" "/test3"
    |> withBody """
    {
        "Fields" : {
            "firstname" : { FieldType : "Text" },
            "lastname" : { FieldType : "Text" },
            "fullname" : {FieldType : "Text", ScriptName : "fullnamescript"}
        },
        "Scripts" : {
            fullnamescript : {
                ScriptType : "ComputedField",
                Source : "return fields[\"firstname\"] + \" \" + fields[\"lastname\"];"
            }
        }
    }
    """
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseBodyIsNull
    |> document
    |> runAssertions

[<Tests>]
let IndexCreationTest5() = 
    example "post-index-5" "Create index by setting all properties"
    |> ofResource "Index"
    |> withDescription """
There are a number of parameters which can be set for a given index. For more information about each parameter please refer to Glossary.
"""
    |> request "POST" "/contact"
    |> withBody (JsonConvert.SerializeObject(Helpers.MockIndexSettings(), jsonSettings))
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseBodyIsNull
    |> document
    |> runAssertions

[<Tests>]
let IndexUpdateTest1() = 
    example "put-index-1" "Updating an existing index"
    |> ofResource "Index"
    |> withDescription """
There are a number of parameters which can be set for a given index. For more information about each parameter please refer to Glossary.
"""
    |> request "PUT" "/test3"
    |> withBody """
{
    "Fields" : {
        "firstname" : { FieldType : "Text" },
        "lastname" : { FieldType : "Text" },
        "fullname" : {FieldType : "Text", ScriptName : "fullnamescript"},
        "desc" : { FieldType : "Stored" },
    },
    "Scripts" : {
        fullnamescript : {
            ScriptType : "ComputedField",
            Source : "return fields[\"firstname\"] + \" \" + fields[\"lastname\"];"
        }
    }
}
"""
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseBodyIsNull
    |> document
    |> runAssertions

[<Tests>]
let IndexUpdateTest2() = 
    example "put-index-2" "Index update request with wrong index name returns error"
    |> ofResource "Index"
    |> withDescription ""
    |> request "PUT" "/indexdoesnotexist"
    |> withBody """
{
    "Fields" : {
        "firstname" : { FieldType : "Text" },
        "lastname" : { FieldType : "Text" },
        "fullname" : {FieldType : "Text", ScriptName : "fullnamescript"},
        "desc" : { FieldType : "Stored" },
    },
    "Scripts" : {
        fullnamescript : {
            ScriptType : "ComputedField",
            Source : "return fields[\"firstname\"] + \" \" + fields[\"lastname\"];"
        }
    }
}
"""
    |> execute
    |> responseStatusEquals HttpStatusCode.BadRequest
    |> responseMatches "ErrorCode" "1000"
    |> document
    |> runAssertions

[<Tests>]
let IndexDeleteTest1() = 
    example "delete-index-1" "Deleting an existing index"
    |> ofResource "Index"
    |> withDescription ""
    |> request "DELETE" "/test3"
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> document
    |> runAssertions

[<Tests>]
let IndexDeleteTest2() = 
    example "delete-index-2" "Deleting an non-existing index will return an error"
    |> ofResource "Index"
    |> withDescription ""
    |> request "DELETE" "/indexDoesNotExist"
    |> execute
    |> responseStatusEquals HttpStatusCode.BadRequest
    |> responseMatches "ErrorCode" "1000"
    |> document
    |> runAssertions

[<Tests>]
let IndexGetTest1() = 
    example "get-index-1" "Getting an index detail by name"
    |> ofResource "Index"
    |> withDescription ""
    |> request "GET" "/contact"
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseMatches "IndexName" "contact"
    |> document
    |> runAssertions

[<Tests>]
let IndexGetTest2() = 
    example "get-index-2" "Getting an index detail by name (non existing index)"
    |> ofResource "Index"
    |> withDescription ""
    |> request "GET" "/indexDoesNotExist"
    |> execute
    |> responseStatusEquals HttpStatusCode.BadRequest
    |> responseMatches "ErrorCode" "1000"
    |> document
    |> runAssertions

[<Tests>]
let IndexExistsTest1() = 
    example "get-index-exists-1" "Checking if an index exists (true case)"
    |> ofResource "Exists"
    |> withDescription ""
    |> request "GET" "/contact/exists"
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseBodyIsNull
    |> document
    |> runAssertions

[<Tests>]
let IndexExistsTest2() = 
    example "get-index-exists-2" "Checking if an index exists (false case)"
    |> ofResource "Exists"
    |> withDescription ""
    |> request "GET" "/indexDoesNotExist/exists"
    |> execute
    |> responseStatusEquals HttpStatusCode.BadRequest
    |> responseMatches "ErrorCode" "1000"
    |> document
    |> runAssertions

[<Tests>]
let IndexStatusTest1() = 
    example "get-index-status-1" "Getting status of an index"
    |> ofResource "Status"
    |> withDescription ""
    |> request "GET" "/test1/status"
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseMatches "Status" "Offline"
    |> document
    |> runAssertions

[<Tests>]
let IndexStatusTest2() = 
    testList "IndexStatusTest 2" [
        yield example "post-index-status-1" "Setting status of an index to on-line"
        |> ofResource "Status"
        |> withDescription ""
        |> request "POST" "/test1/status/online"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> document
        |> runAssertions

        yield example "post-index-status-1" "Index should be online"
        |> ofResource "Status"
        |> withDescription ""
        |> request "GET" "/test1/status"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "Status" "Online"
        |> runAssertions
        ]



[<Tests>]
let IndexStatusTest3() = 
    testList "IndexStatusTest3" [
        yield example "post-index-status-2" "Setting status of an index to off-line"
        |> ofResource "Status"
        |> withDescription ""
        |> request "POST" "/test1/status/offline"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> document
        |> runAssertions

        yield example "post-index-status-2" "Index should be offline"
        |> ofResource "Status"
        |> withDescription ""
        |> request "GET" "/test1/status"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "Status" "Offline"
        |> runAssertions
    ]

[<Tests>]
let IndexDocumentsTest1() = 
    example "post-index-document-id-1" "Add a document to an index"
    |> ofResource "Documents"
    |> withDescription ""
    |> request "POST" "/contact/documents/51"
    |> withBody """
    {
        "firstname" : "Seemant",
        "lastname" : "Rajvanshi"
    }
"""
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseBodyIsNull
    |> document
    |> runAssertions

[<Tests>]
let IndexDocumentsTest2() = 
    Thread.Sleep(5000)
    example "get-index-document-id-1" "Get a document by an id from an index"
    |> ofResource "Documents"
    |> withDescription ""
    |> request "GET" "/contact/documents/51"
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseMatches Constants.IdField "51"
    |> document
    |> runAssertions

[<Tests>]
let IndexDocumentsTest3() = 
    example "put-index-document-id-1" "Update a document by id to an index"
    |> ofResource "Documents"
    |> withDescription ""
    |> request "PUT" "/contact/documents/51"
    |> withBody """
    {
        "firstname" : "Seemant",
        "lastname" : "Rajvanshi"
    }
"""
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseBodyIsNull
    |> document
    |> runAssertions

[<Tests>]
let IndexDocumentsTest4() = 
    example "delete-index-document-id-1" "Delete a document by id from an index"
    |> ofResource "Documents"
    |> withDescription ""
    |> request "DELETE" "/contact/documents/51"
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseBodyIsNull
    |> document
    |> runAssertions

[<Tests>]
let IndexDocumentsBulkLoadingTest() =
    // Bulk generate and add data for search testing
    for id, records in Helpers.GenerateTestDataLines(Helpers.MockTestData) do 
        example "will not document" "Bulk index"
        |> ofResource "Documents"
        |> request "POST" ("/contact/documents/" + id)
        |> withBody (JsonConvert.SerializeObject(records, jsonSettings))
        |> execute
        |> ignore
    testCase "" <| fun _ -> Assert.AreEqual(1, 1)

[<Tests>]
let IndexDocumentsTest5() = 
    Thread.Sleep(5000)
    example "get-index-document-1" "Get top 10 documents from an index"
    |> ofResource "Documents"
    |> withDescription ""
    |> request "GET" "/contact/documents"
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseContainsHeader "RecordsReturned" "10"
    |> responseContainsHeader "TotalAvailable" "50"
    |> document
    |> runAssertions

[<Tests>]
let SearchTest1() = 
    let query = new SearchQuery("contact", "firstname = 'Kathy' and lastname = 'Banks'")
    example "post-index-search-1" "Search document where firstname = 'Kathy' and lastname = 'Banks'"
    |> ofResource "Search"
    |> withDescription "Refer to test data to verify the result."
    |> request "POST" "/contact/search"
    |> withBody (JsonConvert.SerializeObject(query, jsonSettings))
    |> execute
    |> responseStatusEquals HttpStatusCode.OK
    |> responseMatches "RecordsReturned" "1"
    |> responseMatches "TotalAvailable" "1"
    |> document
    |> runAssertions

let testRunHelper() = IndexCreationTest1()
