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

let GetBasicIndexSettings() = 
    let index = new Index()
    index.IndexName <- Guid.NewGuid().ToString("N")
    index.Online <- true
    index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
    index.Fields.Add("gender", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("title", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("givenname", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("middleinitial", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("surname", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("streetaddress", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("city", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("state", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("zipcode", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("country", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("emailaddress", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("username", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("password", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("cctype", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("ccnumber", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("occupation", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("cvv2", new FieldProperties(FieldType = FieldType.Int))
    index.Fields.Add("nationalid", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("ups", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("company", new FieldProperties(FieldType = FieldType.Stored))
    index.Fields.Add("pounds", new FieldProperties(FieldType = FieldType.Double))
    index.Fields.Add("centimeters", new FieldProperties(FieldType = FieldType.Int))
    index.Fields.Add("guid", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("latitude", new FieldProperties(FieldType = FieldType.Double))
    index.Fields.Add("longitude", new FieldProperties(FieldType = FieldType.Double))
    index.Fields.Add("importdate", new FieldProperties(FieldType = FieldType.Date))
    index.Fields.Add("timestamp", new FieldProperties(FieldType = FieldType.DateTime))
    // Computed fields
    index.Fields.Add("fullname", new FieldProperties(FieldType = FieldType.Text, ScriptName = "fullname"))
    index.Scripts.Add
        ("fullname", 
         new ScriptProperties("""return fields["givenname"] + " " + fields["surname"];""", ScriptType.ComputedField))
    let searchProfileQuery = 
        new SearchQuery(index.IndexName, "givenname = '' AND surname = '' AND cvv2 = '1' AND topic = ''")
    searchProfileQuery.MissingValueConfiguration.Add("givenname", MissingValueOption.ThrowError)
    searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
    searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
    index.SearchProfiles.Add("test1", searchProfileQuery)
    index

let url = "http://localhost:9800"
let rootFolder = @"E:\Python27\Scripts\pelican\Scripts\OneDrive\Sites\documentation\content\posts\requests"

//let rootFolder = @"C:\Python27\Scripts\pelican\Scripts\SkyDrive\Sites\documentation\content\requests"
let request uri httpMethod (body : string option) (result : ResizeArray<string>) = 
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

let document (result : ResizeArray<string>) = 
    result.Add("```")
    let fileName = result.[4].Substring(result.[4].IndexOf(":") + 2)
    let path = Path.Combine(rootFolder, fileName + ".md")
    File.WriteAllLines(path, result)

let id id (result : ResizeArray<string>) = 
    result.Add("Slug: " + id)
    result.Add("Date: 2010-12-03 10:20")
    result

let example name = 
    let result = new ResizeArray<string>()
    result.Add("Title: " + name)
    result.Add("Category: examples")
    result.Add("Method: Example")
    result.Add("Uri: " + name)
    result

let description desc (result : ResizeArray<string>) = 
    result.Add("<!--- start -->")
    result.Add(desc)
    result.Add("```javascript")
    result

let generateDocumentation() = 
    let serverSettings = GetServerSettings(ConfFolder.Value + "\\Config.json")
    let node = new NodeService(serverSettings, true)
    node.Start()
    let mutable response = Unchecked.defaultof<_>
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Create index without any field"
    |> id "index-create-1"
    |> description """
The newly created index will be offline as the Online parameter is set to false as default. An index has to be opened after creation to enable indexing.
"""
    |> request "/test1" "POST" None
    |> document
    example "Create index with two field 'firstname' & 'lastname'"
    |> id "index-create-2"
    |> description """
All field names should be lower case and should not contain any spaces. This is to avoid case based mismatching on field names. Fields have many 
other configurable properties but Field Type is the only mandatory parameter. Refer to Index Field for more information about field properties.
"""
    |> request "/test2" "POST" (Some(""" 
    {
        "Fields" : {
            "firstname" : { FieldType : "Text" },
            "lastname" : { FieldType : "Text" }
        }
    }
    """))
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Create index with computed field"
    |> id "index-create-3"
    |> description """
Fields can be dynamic in nature and can be computed at index time from the passed data. Computed field requires custom scripts which defines the 
field data creation logic. Let’s create an index field called fullname which is a concatenation of ‘firstname’ and ‘lastname’.

Computed fields requires ScriptName property to be set in order load a custom script. FlexSearch scripts are 
dynamically compiled to .net dlls so performance wise they are similar to native .net code. Scripts are written 
in C#. But it would be difficult to write complex scripts in single line to pass to the Script source, that 
is why Flex supports Multi-line and File based scripts. Refer to Script for more information about scripts.
"""
    |> request "/test3" "POST" (Some(""" 
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
    """))
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    let indexSettings = GetBasicIndexSettings()
    indexSettings.IndexName <- "test31"
    example "Create index by setting all properties"
    |> id "index-create-4"
    |> description """
There are a number of parameters which can be set for a given index. For more information about each parameter please refer to Glossary.
"""
    |> request "/test31" "POST" (Some(JsonConvert.SerializeObject(indexSettings, jsonSettings)))
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Updating an existing index"
    |> id "update-index-1"
    |> description """

"""
    |> request "/test3" "PUT" (Some(""" 
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
        """))
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Deleting an existing index"
    |> id "delete-index-1"
    |> description """

"""
    |> request "/test31" "DELETE" None
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Deleting an non-existing index will return an error "
    |> id "delete-index-2"
    |> description """

"""
    |> request "/indexdoesnotexist" "DELETE" None
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Getting an index detail by name"
    |> id "get-index-1"
    |> description """

"""
    |> request "/test3" "GET" None
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Getting an index detail by name (non existing index)"
    |> id "get-index-2"
    |> description """

"""
    |> request "/indexdoesnotexist" "GET" None
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Checking if an index exists (true case)"
    |> id "index-exists-1"
    |> description """

"""
    |> request "/test1/exists" "GET" None
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Checking if an index exists (false case)"
    |> id "index-exists-2"
    |> description """

"""
    |> request "/indexdoesnotexist/exists" "GET" None
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Getting status of an index"
    |> id "index-status-1"
    |> description """

"""
    |> request "/test1/status" "GET" None
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Setting status of an index to on-line"
    |> id "index-status-2"
    |> description """

"""
    |> request "/test1/status/online" "POST" None
    |> document
    // -----------------------------------------------
    // Example
    // -----------------------------------------------
    example "Setting status of an index to off-line"
    |> id "index-status-3"
    |> description """

"""
    |> request "/test1/status/offline" "POST" None
    |> document
    printfn "Finished document generation."
    ()
