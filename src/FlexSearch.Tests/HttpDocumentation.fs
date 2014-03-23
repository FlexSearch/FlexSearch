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

let mutable startDate = DateTime.Parse("2012-03-23")

let getDate() = 
    startDate <- startDate.AddDays(-1.0)
    startDate.ToString("yyyy-MM-dd")

/// <summary>
/// Generate Api glossary documentation
/// </summary>
[<Tests>]
let GenerateApiDocumentation() = 
    let file = File.ReadAllLines(Helpers.DocumentationConf.ApiFile)
    let output = new ResizeArray<string>()
    let mutable title = ""
    let mutable insideGroup = false
    for line in file do
        if line.StartsWith("##") then 
            insideGroup <- true
            output.Add("<!--- start -->")
            title <- line.Substring(3)
        else if line.StartsWith("//```") then 
            insideGroup <- false
            output.Add("```")
            output.Insert(0, "Slug: " + title)
            output.Insert(0, "Date: " + getDate())
            output.Insert(0, "Category: Glossary")
            output.Insert(0, "Method: Glossary")
            output.Insert(0, "Uri: " + title)
            output.Insert(0, "Title: " + title)
            File.WriteAllLines
                (Path.Combine(Helpers.DocumentationConf.DocumentationFolder, "glossary", title + ".md"), 
                 output.ToArray())
            output.Clear()
        else if line.StartsWith("*/") && insideGroup then ()
        else 
            if insideGroup then output.Add(line)
    printfn "Finished Api document generation."
    testCase "" <| fun _ -> Assert.AreEqual(1, 1)

/// <summary>
/// Represent a resource to document
/// </summary>
type ResourceDocumentation = 
    { Title : string
      Category : string
      Method : string
      Uri : string
      Examples : string list
      Dtos : string list
      Description : string }

let resource (category : string) (title : string) = 
    { Title = title
      Category = category
      Method = "post"
      Uri = "{indexName}/"
      Examples = []
      Dtos = []
      Description = "" }

let request (meth : string) (uri : string) (result : ResourceDocumentation) = 
    { result with Method = meth
                  Uri = uri }

let examples examples (result : ResourceDocumentation) = { result with Examples = examples }
let dtos dtos (result : ResourceDocumentation) = { result with Dtos = dtos }
let description description (result : ResourceDocumentation) = { result with Description = description }

let document (result : ResourceDocumentation) = 
    let output = new ResizeArray<string>()
    output.Add(result.Description)
    if result.Dtos.Count() > 0 then output.Add("## Objects Used")
    for exampleName in result.Dtos do
        let content = 
            File.ReadAllLines
                (Path.Combine(Helpers.DocumentationConf.DocumentationFolder, "glossary", exampleName + ".md"))
        let mutable inBody = false
        for line in content do
            if line.StartsWith("Title") then 
                output.Add("### " + (line.Substring(line.IndexOf(":") + 1)))
                output.Add("")
            else if line.StartsWith("<!---") then inBody <- true
            else 
                if inBody then output.Add(line)
    if result.Examples.Count() <> 0 then output.Add("## Usage Examples")
    for exampleName in result.Examples do
        let content = 
            File.ReadAllLines
                (Path.Combine(Helpers.DocumentationConf.DocumentationFolder, "requests", exampleName + ".md"))
        let mutable inBody = false
        for line in content do
            if line.StartsWith("Title") then 
                output.Add("### " + (line.Substring(line.IndexOf(":") + 1)))
                output.Add("")
            else if line.StartsWith("<!---") then inBody <- true
            else 
                if inBody then output.Add(line)
    output.Insert(0, "Slug: " + result.Title)
    output.Insert(0, "Date: " + getDate())
    output.Insert(0, "Category: " + result.Category)
    output.Insert(0, "Method: " + result.Method)
    output.Insert(0, "Uri: " + result.Uri)
    output.Insert(0, "Title: " + result.Title)
    File.WriteAllLines
        (Path.Combine(Helpers.DocumentationConf.DocumentationFolder, "resources", result.Title + ".md"), 
         output.ToArray())
    testCase "" <| fun _ -> Assert.AreEqual(1, 1)

[<Tests>]
let IndexIntroductionDocumentation() = 
    resource "Index" "index-introduction"
    |> request "introduction" "Index Basics"
    |> examples []
    |> dtos 
           [ "Index"; "AnalyzerProperties"; "Tokenizer"; "TokenFilter"; "IndexConfiguration"; "FieldProperties"; 
             "FieldType"; "FieldTermVector"; "ScriptProperties"; "ScriptType" ;"SearchProfile"; "SearchQuery"; "ShardConfiguration" ]
    |> description """ 

[TOC]

FlexSearch index is a logical index built on top of Lucene's index in a manner to 
support features like schema and sharding. So in this sense a FlexSearch index 
consists of multiple Lucene's index. Also, each FlexSearch shard is a valid Lucene 
index.
    """
    |> document

[<Tests>]
let GetIndexDocumentation() = 
    resource "Index" "get-index"
    |> request "get" "{indexName}/"
    |> examples [ "get-index-1"; "get-index-2" ]
    |> description """ 
Fetch details of an existing index. 
    """
    |> document

[<Tests>]
let PostIndexDocumentation() = 
    resource "Index" "post-index"
    |> request "post" "{indexName}/"
    |> examples [ "post-index-1"; "post-index-2"; "post-index-3"; "post-index-4"; "post-index-5" ]
    |> dtos [ "Index"; "ShardConfiguration" ]
    |> description """ 
Creates a new index.     
    """
    |> document

[<Tests>]
let PutIndexDocumentation() = 
    resource "Index" "put-index"
    |> request "put" "{indexName}/"
    |> examples [ "put-index-1"; "put-index-2" ]
    |> description """ 
Update an existing index.
    """
    |> document

[<Tests>]
let DeleteIndexDocumentation() = 
    resource "Index" "delete-index"
    |> request "delete" "{indexName}/"
    |> examples [ "delete-index-1"; "delete-index-2" ]
    |> description """ 
Delete an existing index. This will also remove the data from the physical disk.  
    """
    |> document

[<Tests>]
let GetIndexStatusDocumentation() = 
    resource "Status" "get-index-status"
    |> request "get" "{indexName}/status"
    |> examples [ "get-index-status-1" ]
    |> description """ 
Get the status of an existing index.
    """
    |> document

[<Tests>]
let PostIndexStatusDocumentation() = 
    resource "Status" "post-index-status"
    |> request "post" "{indexName}/status/{online|offline}"
    |> examples [ "post-index-status-1"; "post-index-status-2" ]
    |> description """ 
Set the status of an existing index to online or offline.
    """
    |> document

[<Tests>]
let GetIndexExistsDocumentation() = 
    resource "Exists" "get-index-exists"
    |> request "get" "{indexName}/exists"
    |> examples [ "get-index-exists-1"; "get-index-exists-2" ]
    |> description """ 
Check if a given index exists or not.
    """
    |> document

[<Tests>]
let DocumentIntroductionDocumentation() = 
    resource "Document" "document-introduction"
    |> request "introduction" "Document basics"
    |> dtos [ "Document" ]
    |> description """ 


In FlexSearch a document represents the basic unit of information which can be added or 
retrieved from the index. A document consists of several fields. A field represents the 
actual data to be indexed. In database analogy an index can be considered as a table while 
a document is a row of that table. Like a table a FlexSearch document requires a fix 
schema and all fields should have a field type.

Fields can contain different kinds of data. A name field, for example, is text 
(character data). A shoe size field might be a floating point number so that it could 
contain values like 6 and 9.5. Obviously, the definition of fields is flexible (you 
could define a shoe size field as a text field rather than a floating point number, for 
example), but if you define your fields correctly, FlexSearch will be able to interpret 
them correctly and your users will get better results when they perform a query.

You can tell FlexSearch about the kind of data a field contains by specifying its field 
type. The field type tells FlexSearch how to interpret the field and how it can be queried. 
When you add a document, FlexSearch takes the information in the document's fields and 
adds that information to an index. When you perform a query, FlexSearch can quickly consult 
the index and return the matching documents.

Field Analysis Field analysis tells FlexSearch what to do with incoming data when building 
an index. A more accurate name for this process would be processing or even digestion, but 
the official name is analysis. Consider, for example, a biography field in a person document. 
Every word of the biography must be indexed so that you can quickly find people whose lives
have had anything to do with ketchup or dragonflies or cryptography.

However, a biography will likely contains lots of words you don't care about and don't want
clogging up your index, words like 'the', 'a', 'to', and so forth. Furthermore, suppose the
biography contains the word 'Ketchup', capitalized at the beginning of a sentence. If a user 
makes a query for 'ketchup', you want FlexSearch to tell you about the person even though 
the biography contains the capitalized word.

The solution to both these problems is field analysis. For the biography field, you can tell
FlexSearch how to break apart the biography into words. You can tell FlexSearch that you 
want to make all the words lower case, and you can tell FlexSearch to remove accents marks. 
Field analysis is an important part of a field type.

    """
    |> document

[<Tests>]
let GetIndexDocumentIdDocumentation() = 
    resource "Document" "get-index-document-id"
    |> request "get" "{indexName}/documents/{id}"
    |> examples [ "get-index-document-id-1" ]
    |> description """ 
Get a document from an existing index.
    """
    |> document

[<Tests>]
let GetIndexDocumentDocumentation() = 
    resource "Document" "get-index-document"
    |> request "get" "{indexName}/documents/"
    |> examples [ "get-index-document-1" ]
    |> description """ 
Returns top 10 documents from the index.
    """
    |> document

[<Tests>]
let PostIndexDocumentIdDocumentation() = 
    resource "Document" "post-index-document-id"
    |> request "post" "{indexName}/documents/{id}"
    |> examples [ "post-index-document-id-1" ]
    |> description """ 
Add a new document to an existing index. This will always add a new document even if the id already exists. In case you want to perform add or update operation, use the PUT method.       
    """
    |> document

[<Tests>]
let PutIndexDocumentIdDocumentation() = 
    resource "Document" "put-index-document-id"
    |> request "put" "{indexName}/documents/{id}"
    |> examples [ "put-index-document-id-1" ]
    |> description """ 
Update or create a document in an existing index.
    """
    |> document

[<Tests>]
let DeleteIndexDocumentIdDocumentation() = 
    resource "Document" "delete-index-document-id"
    |> request "delete" "{indexName}/documents/{id}"
    |> examples [ "delete-index-document-id-1" ]
    |> description """ 
Delete a document in an existing index.
    """
    |> document

[<Tests>]
let SearchIntroductionDocumentation() = 
    resource "Search" "search-introduction"
    |> request "introduction" "Search basics"
    |> dtos [ "SearchQuery"; "SearchResults"; "Document" ]
    |> description """ 
    """
    |> document

[<Tests>]
let PostIndexSearchDocumentation() = 
    resource "Search" "post-index-search"
    |> request "post" "{indexName}/search/"
    |> examples [ "post-index-search-1" ]
    |> description """ 
Search for documents in an index.
    """
    |> document
