module HttpTests

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
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
      Output : ResizeArray<string>
      OutputResponse : ResizeArray<string> }

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
          Output = new ResizeArray<string>()
          OutputResponse = new ResizeArray<string>() }
    result.Output.Add(name)
    result.Output.Add
        ("''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''")
    result.Output.Add("")
    result

let ofResource (name : string) (result : Example) = { result with Resource = name }

let withDescription (desc : string) (result : Example) = 
    result.Output.Add(desc)
    result.Output.Add("")
    result.Output.Add(sprintf ".. literalinclude:: example-%s.txt" result.Id)
    result.Output.Add("\t:language: javascript")
    result.Output.Add("")
    { result with Description = desc }

let request (meth : string) (uri : string) (result : Example) = 
    { result with Method = meth
                  Uri = uri }

let withBody (body : string) (result : Example) = { result with Requestbody = Some(body) }

// ----------------------------------------------------------------------------
// Test assertions
// ----------------------------------------------------------------------------
let responseStatusEquals (status : HttpStatusCode) (result : Example) = 
    Assert.Equal<HttpStatusCode>(status, result.Response.StatusCode) // "Status code does not match"
    result

let responseContainsHeader (header : string) (value : string) (result : Example) = 
    Assert.Equal<string>(value, result.Response.Headers.Get(header)) // "Header value does not match"
    result

let responseMatches (select : string) (expected : string) (result : Example) = 
    let value = JObject.Parse(result.ResponseBody)
    Assert.Equal<string>(expected, value.SelectToken(select).ToString()) // "Response does not match"
    result

let responseShouldContain (value : string) (result : Example) = 
    Assert.True(result.ResponseBody.Contains(value), "Response does contain the required value")
    result

let responseContainsProperty (group : string) (key : string) (property : string) (expected : string) (result : Example) = 
    let value = JObject.Parse(result.ResponseBody)
    Assert.Equal<string>(expected, value.SelectToken(group).[key].[property].ToString()) //"Response does contain the required property"
    result

let responseBodyIsNull (result : Example) = 
    Assert.True(String.IsNullOrWhiteSpace(result.ResponseBody), "Response should not contain body")
    result

// ----------------------------------------------------------------------------
// Test logic
// ----------------------------------------------------------------------------
let execute (result : Example) = 
    result.OutputResponse.Add((sprintf "%s %s HTTP/1.1" result.Method result.Uri))
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
            result.OutputResponse.Add(sprintf "%s:%s" headerCollection.Keys.[i] (headerCollection.GetValues(i).[0]))
    
    let print (resp : HttpWebResponse) = 
        printHeaders (req.Headers)
        if result.Requestbody.IsSome then 
            let parsedJson = JsonConvert.DeserializeObject(result.Requestbody.Value)
            result.OutputResponse.Add(JsonConvert.SerializeObject(parsedJson, Formatting.Indented))
        result.OutputResponse.Add("")
        result.OutputResponse.Add("")
        result.OutputResponse.Add((sprintf "HTTP/1.1 %i %s" (int resp.StatusCode) (resp.StatusCode.ToString())))
        printHeaders (resp.Headers)
        if req.HaveResponse then 
            let stream = resp.GetResponseStream()
            let reader = new StreamReader(stream)
            let responseBody = reader.ReadToEnd()
            result.ResponseBody <- responseBody
            let parsedJson = JsonConvert.DeserializeObject(responseBody)
            if parsedJson <> Unchecked.defaultof<_> then 
                result.OutputResponse.Add("")
                result.OutputResponse.Add(sprintf "%s" (JsonConvert.SerializeObject(parsedJson, Formatting.Indented)))
    
    try 
        result.Response <- req.GetResponse() :?> HttpWebResponse
        print result.Response
    with :? WebException as e -> 
        result.Response <- e.Response :?> HttpWebResponse
        print result.Response
    for line in result.OutputResponse do
        printfn "%s" line
    result

// ----------------------------------------------------------------------------
// Output logic
// ----------------------------------------------------------------------------
let document (result : Example) = 
    result.Output.Add(" ")
    let path = Path.Combine(Helpers.DocumentationConf.DocumentationFolder, "example-" + result.Id + ".rst")
    let examplePath = Path.Combine(Helpers.DocumentationConf.DocumentationFolder, "example-" + result.Id + ".txt")
    if Directory.Exists(Helpers.DocumentationConf.DocumentationFolder) then 
        File.WriteAllLines(path, result.Output)
        File.WriteAllLines(examplePath, result.OutputResponse)
    result

// ----------------------------------------------------------------------------
// Test server initialization
// ----------------------------------------------------------------------------
/// <summary>
/// Test setup fixture to use with Xunit IUseFixture
/// </summary>
type IntegrationIndexFixture() = 
    let mutable serverRunning = false
    member this.Setup() = 
        if serverRunning <> true then 
            let serverSettings = new ServerSettings()
            let container = Main.GetContainer(serverSettings, true)
            let indexService = container.Resolve<IIndexService>()
            let index = Helpers.MockIndexSettings()
            index.IndexName <- "contact"
            AddTestDataToIndex
                (index, Helpers.MockTestData, container.Resolve<IDocumentService>(), container.Resolve<IIndexService>())
            let httpFactory = container.Resolve<IFlexFactory<HttpModuleBase>>()
            let httpServer = new Owin.Server(indexService, httpFactory) :> IServer
            httpServer.Start()
            serverRunning <- true

/// <summary>
/// Base for creating all Xunit based indexing integration tests
/// </summary>
[<AbstractClass>]
type IntegrationTestBase() = 
    interface IUseFixture<IntegrationIndexFixture> with
        member this.SetFixture(data) = data.Setup()

type ``REST Service Tests``() = 
    inherit IntegrationTestBase()
    
    [<Fact>]
    let ``Index creation test 1``() = 
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
    
    [<Fact>]
    let ``Index creation test 2``() = 
        example "post-index-1" "Create index without any field"
        |> ofResource "Index"
        |> withDescription """
    The newly created index will be offline as the Online parameter is set to false as default. An index has to be opened after creation to enable indexing.
    """
        |> request "POST" "/test101"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
        example "post-index-2" "Duplicate index cannot be created."
        |> ofResource "Index"
        |> withDescription """
    The newly created index will be offline as the Online parameter is set to false as default. An index has to be opened after creation to enable indexing.
    """
        |> request "POST" "/test101"
        |> execute
        |> responseStatusEquals HttpStatusCode.BadRequest
        |> responseMatches "ErrorCode" "1002"
        |> document
    
    [<Fact>]
    let ``Index creation test 3``() = 
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
    
    [<Fact>]
    let ``Create update and delete an index``() = 
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
        |> ignore
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
        |> ignore
        example "delete-index-1" "Deleting an existing index"
        |> ofResource "Index"
        |> withDescription ""
        |> request "DELETE" "/test3"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> document
    
    [<Fact>]
    let IndexCreationTest5() = 
        example "post-index-5" "Create index by setting all properties"
        |> ofResource "Index"
        |> withDescription """
    There are a number of parameters which can be set for a given index. For more information about each parameter please refer to Glossary.
    """
        |> request "POST" "/contact123"
        |> withBody (JsonConvert.SerializeObject(Helpers.MockIndexSettings(), jsonSettings))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> document
    
    [<Fact>]
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
    
    [<Fact>]
    let IndexDeleteTest2() = 
        example "delete-index-2" "Deleting an non-existing index will return an error"
        |> ofResource "Index"
        |> withDescription ""
        |> request "DELETE" "/indexDoesNotExist"
        |> execute
        |> responseStatusEquals HttpStatusCode.BadRequest
        |> responseMatches "ErrorCode" "1000"
        |> document
    
    [<Fact>]
    let IndexGetTest1() = 
        example "get-index-1" "Getting an index detail by name"
        |> ofResource "Index"
        |> withDescription ""
        |> request "GET" "/contact"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "IndexName" "contact"
        |> document
    
    [<Fact>]
    let IndexGetTest2() = 
        example "get-index-2" "Getting an index detail by name (non existing index)"
        |> ofResource "Index"
        |> withDescription ""
        |> request "GET" "/indexDoesNotExist"
        |> execute
        |> responseStatusEquals HttpStatusCode.BadRequest
        |> responseMatches "ErrorCode" "1000"
        |> document
    
    [<Fact>]
    let IndexExistsTest1() = 
        example "get-index-exists-1" "Checking if an index exists (true case)"
        |> ofResource "Exists"
        |> withDescription ""
        |> request "GET" "/contact/exists"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> document
    
    [<Fact>]
    let IndexExistsTest2() = 
        example "get-index-exists-2" "Checking if an index exists (false case)"
        |> ofResource "Exists"
        |> withDescription ""
        |> request "GET" "/indexDoesNotExist/exists"
        |> execute
        |> responseStatusEquals HttpStatusCode.BadRequest
        |> responseMatches "ErrorCode" "1000"
        |> document
    
    [<Fact>]
    let IndexStatusTest() = 
        example "" ""
        |> request "POST" "/indexstatustest"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
        example "get-index-status-2" "Getting status of an index (offline)"
        |> ofResource "Status"
        |> withDescription ""
        |> request "GET" "/indexstatustest/status"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "Status" "Offline"
        |> document
        |> ignore
        example "post-index-status-1" "Setting status of an index to on-line"
        |> ofResource "Status"
        |> withDescription ""
        |> request "POST" "/indexstatustest/status/online"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> document
        |> ignore
        example "get-index-status-1" "Getting status of an index"
        |> ofResource "Status"
        |> withDescription ""
        |> request "GET" "/indexstatustest/status"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "Status" "Online"
        |> document
        |> ignore
        example "post-index-status-2" "Setting status of an index to off-line"
        |> ofResource "Status"
        |> withDescription ""
        |> request "POST" "/indexstatustest/status/offline"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> document
    
    [<Fact>]
    let IndexDocumentsTest() = 
        example "" ""
        |> request "POST" "/documenttestindex"
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
        |> ignore
        example "post-index-document-id-1" "Add a document to an index"
        |> ofResource "Documents"
        |> withDescription ""
        |> request "POST" "/documenttestindex/documents/51"
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
        |> ignore
        Thread.Sleep(5000)
        example "get-index-document-id-1" "Get a document by an id from an index"
        |> ofResource "Documents"
        |> withDescription ""
        |> request "GET" "/documenttestindex/documents/51"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches Constants.IdField "51"
        |> document
        |> ignore
        example "put-index-document-id-1" "Update a document by id to an index"
        |> ofResource "Documents"
        |> withDescription ""
        |> request "PUT" "/documenttestindex/documents/51"
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
        |> ignore
        example "delete-index-document-id-1" "Delete a document by id from an index"
        |> ofResource "Documents"
        |> withDescription ""
        |> request "DELETE" "/documenttestindex/documents/51"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> document
    
    [<Fact>]
    let IndexDocumentsTest5() = 
        example "get-index-document-1" "Get top 10 documents from an index"
        |> ofResource "Documents"
        |> withDescription ""
        |> request "GET" "/contact/documents"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        //|> responseContainsHeader "RecordsReturned" "10"
        //|> responseContainsHeader "TotalAvailable" "50"
        |> document
    
    [<Fact>]
    let SearchTermQueryTest1() = 
        example "post-index-search-termquery-1" "Term search using ``=`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to match all documents where firstname = 'Kathy' and lastname = 'Banks'

        firstname = 'Kathy' and lastname = 'Banks'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "firstname = 'Kathy' and lastname = 'Banks'"
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "RecordsReturned" "1"
        |> responseMatches "TotalAvailable" "1"
        |> document
    
    [<Fact>]
    let SearchTermQueryTest2() = 
        example "post-index-search-termquery-2" "Term search using ``eq`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to match all documents where firstname eq 'Kathy' and lastname eq 'Banks'

        firstname eq 'Kathy' and lastname eq 'Banks'
    """
        |> request "POST" "/contact/search?c=*"
        |> withBody """
    {
      "QueryString": "firstname eq 'Kathy' and lastname eq 'Banks'",
      "ReturnFlatResult": true
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "1"
        |> responseContainsHeader "TotalAvailable" "1"
        |> document
    
    [<Fact>]
    let SearchFuzzyQueryTest1() = 
        example "post-index-search-fuzzyquery-1" "Fuzzy search using ``fuzzy`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to fuzzy match all documents where firstname is 'Kathy'

        firstname fuzzy 'Kathy'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "firstname fuzzy 'Kathy'"
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseMatches "RecordsReturned" "3"
        |> responseMatches "TotalAvailable" "3"
        |> document
    
    [<Fact>]
    let SearchFuzzyQueryTest2() = 
        example "post-index-search-fuzzyquery-2" "Fuzzy search using ``~=`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to fuzzy match all documents where firstname is 'Kathy'

        firstname ~= 'Kathy'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "firstname ~= 'Kathy'",
      "ReturnFlatResult": true
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "3"
        |> responseContainsHeader "TotalAvailable" "3"
        |> document
    
    [<Fact>]
    let SearchFuzzyQueryTest3() = 
        example "post-index-search-fuzzyquery-3" "Fuzzy search using slop parameter"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to fuzzy match all documents where firstname is 'Kathy' and slop is 2

        firstname ~= 'Kathy'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "firstname ~= 'Kathy' {slop : '2'}",
      "ReturnFlatResult": true
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "3"
        |> responseContainsHeader "TotalAvailable" "3"
        |> document
    
    [<Fact>]
    let SearchPhraseQueryTest1() = 
        example "post-index-search-phrasequery-1" "Phrase search using ``match`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to fuzzy match all documents where description is 'Nunc purus'

        description match 'Nunc purus'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "description match 'Nunc purus'",
      "ReturnFlatResult": true
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "4"
        |> responseContainsHeader "TotalAvailable" "4"
        |> document
    
    [<Fact>]
    let SearchWildCardQueryTest1() = 
        example "post-index-search-wildcardquery-1" "Wildcard search using ``like`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to fuzzy match all documents where firstname is like 'Ca*'

        firstname like 'Ca*'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "firstname like 'ca*'",
      "ReturnFlatResult": true
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "3"
        |> responseContainsHeader "TotalAvailable" "3"
        |> document
    
    [<Fact>]
    let SearchWildCardQueryTest2() = 
        example "post-index-search-wildcardquery-2" "Wildcard search using ``%=`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to fuzzy match all documents where firstname is like 'Ca*'

        firstname %= 'Ca*'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "firstname %= 'Ca*'",
      "ReturnFlatResult": true
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "3"
        |> responseContainsHeader "TotalAvailable" "3"
        |> document
    
    [<Fact>]
    let SearchWildCardQueryTest3() = 
        example "post-index-search-wildcardquery-3" "Wildcard search using ``%=`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to fuzzy match all documents where firstname is like 'Cat?y'. This can
    be used to match one character.

        firstname %= 'Cat?y'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "firstname %= 'Cat?y'",
      "ReturnFlatResult": true
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "1"
        |> responseContainsHeader "TotalAvailable" "1"
        |> document
    
    [<Fact>]
    let SearchRegexQueryTest1() = 
        example "post-index-search-regexquery-1" "Regex search using ``regex`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to fuzzy match all documents where firstname is like '[ck]athy'. This can
    be used to match one character.

        firstname regex '[ck]Athy'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "firstname regex '[ck]Athy'",
      "ReturnFlatResult": true
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "3"
        |> responseContainsHeader "TotalAvailable" "3"
        |> document
    
    [<Fact>]
    let SearchMatchallQueryTest1() = 
        example "post-index-search-matchallquery-1" "Match all search using ``matchall`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to to match all documents in the index.

        firstname matchall '*'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "firstname matchall '*'",
      "ReturnFlatResult": true,
      Count: 1
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "1"
        |> responseContainsHeader "TotalAvailable" "50"
        |> document
    
    [<Fact>]
    let SearchNumericRangeQueryTest1() = 
        example "post-index-search-numericrangequery-1" "Range search using ``>`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to to match all documents with cvv2 greater than 100 in the index.

        cvv2 > '100'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "cvv2 > '100'",
      "ReturnFlatResult": true,
      Count: 1
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "1"
        |> responseContainsHeader "TotalAvailable" "48"
        |> document
    
    [<Fact>]
    let SearchNumericRangeQueryTest2() = 
        example "post-index-search-numericrangequery-2" "Range search using ``>=`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to to match all documents with cvv2 greater than or equal to 200 in the index.

        cvv2 >= '200'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "cvv2 >= '200'",
      "ReturnFlatResult": true,
      Count: 1
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "1"
        |> responseContainsHeader "TotalAvailable" "41"
        |> document
    
    [<Fact>]
    let SearchNumericRangeQueryTest3() = 
        example "post-index-search-numericrangequery-3" "Range search using ``<`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to to match all documents with cvv2 less than 150 in the index.

        cvv2 < '150'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "cvv2 < '150'",
      "ReturnFlatResult": true,
      Count: 1
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "1"
        |> responseContainsHeader "TotalAvailable" "7"
        |> document
    
    [<Fact>]
    let SearchNumericRangeQueryTest4() = 
        example "post-index-search-numericrangequery-4" "Range search using ``<=`` operator"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to to match all documents with cvv2 less than or equal to 500 in the index.

        cvv2 <= '500'
    """
        |> request "POST" "/contact/search?c=firstname,lastname"
        |> withBody """
    {
      "QueryString": "cvv2 <= '500'",
      "ReturnFlatResult": true,
      Count: 1
    }    
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseContainsHeader "RecordsReturned" "1"
        |> responseContainsHeader "TotalAvailable" "26"
        |> document
    
    [<Fact>]
    let SearchHighlightFeatureTest1() = 
        let query = new SearchQuery("contact", " description = 'Nullam'")
        let highlight = new List<string>()
        highlight.Add("description")
        query.Highlights <- new HighlightOption(highlight)
        example "post-index-search-highlightfeature-1" "Text highlighting basic example"
        |> ofResource "Search"
        |> withDescription """
    The below is the query to highlight 'Nullam' is description field.

        description = 'Nullam'
    """
        |> request "POST" "/contact/search?c=firstname,lastname,description"
        |> withBody """
    {
      "Count": 2,  
      "Highlights": {
        "FragmentsToReturn": 2,
        "HighlightedFields": [
          "description"
        ],
        "PostTag": "</B>",
        "PreTag": "</B>"
      },
      "QueryString": " description = 'Nullam'",
      }   
    """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> document
