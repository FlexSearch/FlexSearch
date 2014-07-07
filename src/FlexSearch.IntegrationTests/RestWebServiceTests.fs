namespace FlexSearch.IntegrationTests

module ``Rest webservices tests`` = 
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
    open FlexSearch.TestSupport.RestHelpers
   
    [<Theory>][<AutoMockIntegrationData>]
    let ``Accessing server root should return 200`` (server : TestServer) = 
        server
        |> request "GET" "/"
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Creating an index without any parameters should return 200`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Duplicate index cannot be created`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> ignore
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
        |> execute
        |> responseStatusEquals HttpStatusCode.BadRequest
        |> responseMatches "ErrorCode" "1002"
        |> ignore
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Create index with two field 'firstname' & 'lastname'`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
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
    
    [<Theory>][<AutoMockIntegrationData>]
    let ``Create an index with dynamic fields`` (server : TestServer, indexName : Guid) = 
        server
        |> request "POST" ("/indices/" + indexName.ToString("N"))
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
                        Source : "return fields.firstname + \" \" + fields.lastname;"
                    }
                }
        }        
        """
        |> execute
        |> responseStatusEquals HttpStatusCode.OK
        |> responseBodyIsNull
        |> ignore
