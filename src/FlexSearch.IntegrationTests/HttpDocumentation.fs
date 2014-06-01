module HttpDocumentation

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
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
    if result.Examples.Count() <> 0 then 
        output.Add("**Usage Examples**")
        output.Add("")
        output.Add("[TOC]")

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

FlexSearch follows a consistent search dsl to execute all kind of search request. 
This enables a unified search experience for the developers. Before getting into 
the various types of search queries supported by FlexSearch we will cover the basic 
search mechanics.

    """
    |> document

[<Tests>]
let SearchQueryFormatDocumentation() = 
    resource "Search" "search-queryformat"
    |> request "introduction" "Query format"
    |> description """ 

FlexSearch utilizes custom query format inspired by SQL. This is done to
reduce the learning curve when moving to FlexSearch as most of the modern day programmers have
written SQL at some point of time in there life.

The below is the query format:
    
    <search_condition> ::= 
        { [ NOT ] <predicate> | ( <search_condition> ) } 
        [ { AND | OR } [ NOT ] { <predicate> | ( <search_condition> ) } ] 
    [ ,...n ] 

    <predicate> ::= <field name> <operator> <values>

    <values> ::= <value>
                |   <value> <options>

    <value> ::= <single value>
                | <list values>

    <single value> ::= '<any_search_value>'
                    | `<any_search_value> <escape> <any_search_value>'

    <escape> ::= \\'

    <list values> = [ <single value> , <single value> , ..n ]

    <options> ::= { <parameter key> : '<parameter value>' , <parameter key> : '<parameter value>', ...n }

The parser implements operator precedence as NOT >> AND >> OR.

FlexSearch supports a number of query operators, more explanation about these can be accessed
from the query dropdowns.
"""
    |> document


[<Tests>]
let SearchTermQueryDocumentation() = 
    resource "Search" "search-termquery"
    |> request "query" "Term match operator"
    |> examples ["post-index-search-termquery-1" ; "post-index-search-termquery-2"]
    |> description """ 
A Query that matches documents containing a term. 

| Parameter      | Default | Type   | Description                                                                                                                                                                                                                                      |
|----------------|---------|--------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ``clausetype`` | and     | string | In case more than one term is searched then the query is converted into a number of sub-queries and the clausetype operator is used to determine the matching logic. For example an ``and`` clause will match all the terms passed to the query. |

Do not use term query for phrase matches as you might get unexpected results.

    Term match supports both ``=`` and ``eq`` operator.
"""
    |> document


[<Tests>]
let SearchFuzzyQueryDocumentation() = 
    resource "Search" "search-fuzzyquery"
    |> request "query" "Fuzzy operator"
    |> examples ["post-index-search-fuzzyquery-1"; "post-index-search-fuzzyquery-2"; "post-index-search-fuzzyquery-3"]
    |> description """ 
Implements the fuzzy search query. The similarity measurement is based on the Damerau-Levenshtein (optimal string alignment) algorithm. At most, this query will match terms up to 2 edits. Higher distances, are generally not useful and will match a significant amount of the term dictionary. If you really want this, consider using an n-gram indexing technique (such as the SpellChecker in the suggest module) instead.
    
| Parameter        | Default | Type | Description                          |
|------------------|---------|------|--------------------------------------|
| ``prefixlength`` | 0       | int  | Length of common (non-fuzzy) prefix. |
| ``slop``         | 1       | int  | The number of allowed edits          |


    Fuzzy supports both ``"fuzzy`` and ``~=`` operator.

"""
    |> document


[<Tests>]
let SearchPhraseQueryDocumentation() = 
    resource "Search" "search-phrasequery"
    |> request "query" "Phrase match operator"
    |> examples ["post-index-search-phrasequery-1"; ]
    |> description """ 
A Query that matches documents containing a particular sequence of terms. A PhraseQuery is built by QueryParser for input like "new york".

| Parameter | Default | Type | Description                 |
|-----------|---------|------|-----------------------------|
| ``slop``  | 1       | int  | The number of allowed edits |


    Phrase match supports ``"match``.

"""
    |> document


[<Tests>]
let SearchWildcardQueryDocumentation() = 
    resource "Search" "search-wildcardquery"
    |> request "query" "Wildcard operator"
    |> examples ["post-index-search-wildcardquery-1"; "post-index-search-wildcardquery-2"]
    |> description """ 
Implements the wildcard search query. Supported wildcards are \*, which matches 
any character sequence (including the empty one), and ?, which matches any single 
character. '\' is the escape character. Note this query can be slow, as it needs 
to iterate over many terms. In order to prevent extremely slow WildcardQueries, 
a Wildcard term should not start with the wildcard \*

```
Phrase match supports `like` and `%=` operators.
```

```
Like query does not go through analysis phase as the analyzer would remove the special characters. This 
will convert the input to lowercase before comparison.
```
"""
    |> document

[<Tests>]
let SearchRegexQueryDocumentation() = 
    resource "Search" "search-regexquery"
    |> request "query" "Regex operator"
    |> examples ["post-index-search-regexquery-1";]
    |> description """ 
A fast regular expression query based on the org.apache.lucene.util.automaton package.
Comparisons are fast

The term dictionary is enumerated in an intelligent way, to avoid comparisons. See AutomatonQuery for more details.
The supported syntax is documented in the RegExp class. Note this might be different than other regular expression 
implementations. For some alternatives with different syntax, look under the sandbox.

Note this query can be slow, as it needs to iterate over many terms. In order to prevent extremely slow RegexpQueries, a Regexp term should not start with the expression .*



```
Regex supports `regex` operator.
```

```
Regex query does not go through analysis phase as the analyzer would remove the special characters. This 
will convert the input to lowercase before comparison.
```
"""
    |> document

[<Tests>]
let SearchMatchAllQueryDocumentation() = 
    resource "Search" "search-matchallquery"
    |> request "query" "Matchall operator"
    |> examples ["post-index-search-matchallquery-1";]
    |> description """ 
A query that matches all documents. It is a useful query to iterate over all documents in an index.

```
Matchall supports `matchall` operator.
```

"""
    |> document

[<Tests>]
let SearchNumericRangeQueryDocumentation() = 
    resource "Search" "search-numericrangequery"
    |> request "query" "Numeric range operators"
    |> examples ["post-index-search-numericrangequery-1";"post-index-search-numericrangequery-2";"post-index-search-numericrangequery-3";"post-index-search-numericrangequery-4"]
    |> description """ 
A Query that matches numeric values within a specified range. To use this, you must first index the numeric values using Int. DateTime, Date or Double. 

```
Range supports '>', '>=', '<' and '<='  operators.
```
"""
    |> document

[<Tests>]
let SearchHighlightFeatureDocumentation() = 
    resource "Search" "search-highlightfeature"
    |> request "feature" "Text Highlighting"
    |> examples ["post-index-search-highlightfeature-1"]
    |> description """ 
FlexSearch supports text highlighting across all query types provided correct highlighting options are set in 
the request query. Text highlighting is supported only for ``Highlight`` and ``Custom`` field types.

PreTag and PostTag can be specified and the returned result will contain the matched text between pre and post
tags. This is helpful in case the results are to be expressed in a web page.

    `returnFlatResults` should be set to false in order to get highlight results.

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
