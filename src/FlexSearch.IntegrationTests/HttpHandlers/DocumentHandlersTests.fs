namespace FlexSearch.IntegrationTests.Rest

open FlexSearch.Api
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
open FlexSearch.Client

type Dummy() =
    member val DummyProperty = "" with get, set

module ``Document Tests`` = 
    let TestIndex(indexName) = 
        let index = new Index(IndexName = indexName, Online = true)
        index.Fields.Add("firstname", new FieldProperties(FieldType = FieldType.Text))
        index.Fields.Add("lastname", new FieldProperties(FieldType = FieldType.Text))
        index
    
    [<Theory; AutoMockIntegrationData; Example("get-indices-id-documents-1", "")>]
    let ``Get top 10 documents from an index`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        let actual = client.GetTopDocuments("contact", 10).Result
        actual |> ExpectSuccess
        handler |> VerifyHttpCode HttpStatusCode.OK
        Assert.Equal<int>(10, actual.Data.RecordsReturned)
        Assert.Equal<int>(50, actual.Data.TotalAvailable)
    
    [<Theory; AutoMockIntegrationData; Example("post-indices-id-documents-id-2", "")>]
    let ``Add a document to an index`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        client.AddIndex(TestIndex(indexName.ToString("N"))).Result |> ExpectSuccess
        let document = new FlexDocument(indexName.ToString("N"), "1")
        document.Fields.Add("firstname", "Seemant")
        document.Fields.Add("lastname", "Rajvanshi")
        let actual = client.AddDocument(indexName.ToString("N"), document).Result
        actual |> ExpectSuccess
        Assert.Equal<string>("1", actual.Data.Id)
        handler |> VerifyHttpCode HttpStatusCode.Created

    [<Theory; AutoMockIntegrationData>]
    let ``Cannot add a document without an id`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        client.AddIndex(TestIndex(indexName.ToString("N"))).Result |> ExpectSuccess
        let document = new FlexDocument(indexName.ToString("N"), " ")
        client.AddDocument(indexName.ToString("N"), document).Result |> ignore
        handler |> VerifyHttpCode HttpStatusCode.BadRequest
        printfn "%s" (handler.Log().ToString())

    [<Theory; AutoMockIntegrationData; Example("put-indices-id-documents-id-1", "")>]
    let ``Update a document to an index`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        ``Add a document to an index`` (client, indexName, handler)
        let document = new FlexDocument(indexName.ToString("N"), "1")
        document.Fields.Add("firstname", "Seemant")
        document.Fields.Add("lastname", "Rajvanshi1")
        let actual = client.UpdateDocument(indexName.ToString("N"), document).Result
        actual |> ExpectSuccess
        handler |> VerifyHttpCode HttpStatusCode.OK

    [<Theory; AutoMockIntegrationData; Example("get-indices-id-documents-id-1", "")>]
    let ``Get a document from an index`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        let actual = client.GetDocument("contact", "1").Result
        actual |> ExpectSuccess
        Assert.Equal<String>("1", actual.Data.Id)

    [<Theory; AutoMockIntegrationData; Example("get-indices-id-documents-id-2", "")>]
    let ``Non existing document should return Not found`` (client : IFlexClient, indexName : Guid, handler : LoggingHandler) = 
        let actual = client.GetDocument("contact", "55").Result
        handler |> VerifyHttpCode HttpStatusCode.NotFound

