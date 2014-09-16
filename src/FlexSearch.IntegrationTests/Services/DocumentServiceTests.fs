namespace FlexSearch.IntegrationTests.DocumentServiceTests

open Autofac
open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.TestSupport
open Ploeh.AutoFixture.Xunit
open System.Collections.Generic
open System.Linq
open Xunit
open Xunit.Extensions

module ``Versioning tests`` = 
    [<Theory; AutoMockIntegrationData>]
    let ``Cannot create a duplicate document with a timestamp of -1`` (indexService : IIndexService, 
                                                                       documentService : IDocumentService, index : Index) = 
        indexService.AddIndex(index) |> ExpectSuccess
        let document = new FlexDocument(index.IndexName, "1")
        documentService.AddDocument(document) |> ExpectSuccess
        document.TimeStamp <- -1L
        documentService.AddDocument(document) 
        |> ExpectErrorCode(Errors.INDEXING_DOCUMENT_ID_ALREADY_EXISTS |> GenerateOperationMessage)
    
    [<Theory; AutoMockIntegrationData>]
    let ``Cannot create a document with timestamp of 1`` (indexService : IIndexService, 
                                                          documentService : IDocumentService, index : Index) = 
        // TimeStamp of 1 implies that we want to ensure that the document exists which is against the logic of basic create operation
        indexService.AddIndex(index) |> ExpectSuccess
        let document = new FlexDocument(index.IndexName, "1", TimeStamp = 1L)
        documentService.AddDocument(document) 
        |> ExpectErrorCode(Errors.INDEXING_VERSION_CONFLICT_CREATE |> GenerateOperationMessage)
    
    [<Theory; AutoMockIntegrationData>]
    let ``Duplicate document can be created with a timestamp of 0`` (indexService : IIndexService, 
                                                                     documentService : IDocumentService, index : Index) = 
        indexService.AddIndex(index) |> ExpectSuccess
        let document = new FlexDocument(index.IndexName, "1")
        documentService.AddDocument(document) |> ExpectSuccess
        document.TimeStamp <- 0L
        documentService.AddDocument(document) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``For optimistic update the timestamp should match`` (indexService : IIndexService, 
                                                              documentService : IDocumentService, index : Index) = 
        indexService.AddIndex(index) |> ExpectSuccess
        let document = new FlexDocument(index.IndexName, "1")
        let response = documentService.AddDocument(document) |> GetSuccessChoice
        indexService.Refresh(index.IndexName) |> ExpectSuccess
        let timeStamp = (documentService.GetDocument(index.IndexName, "1") |> GetSuccessChoice).TimeStamp
        document.TimeStamp <- timeStamp
        documentService.AddOrUpdateDocument(document) |> ExpectSuccess
        document.TimeStamp <- 1000L
        documentService.AddOrUpdateDocument(document) 
        |> ExpectErrorCode(Errors.INDEXING_VERSION_CONFLICT |> GenerateOperationMessage)
    
    [<Theory; AutoMockIntegrationData>]
    let ``Cannot update a document with wrong timestamp`` (indexService : IIndexService, 
                                                           documentService : IDocumentService, index : Index) = 
        indexService.AddIndex(index) |> ExpectSuccess
        let document = new FlexDocument(index.IndexName, "1")
        let response = documentService.AddDocument(document) |> GetSuccessChoice
        indexService.Refresh(index.IndexName) |> ExpectSuccess
        let timeStamp = (documentService.GetDocument(index.IndexName, "1") |> GetSuccessChoice).TimeStamp
        document.TimeStamp <- 2L
        documentService.AddOrUpdateDocument(document) 
        |> ExpectErrorCode(Errors.INDEXING_VERSION_CONFLICT |> GenerateOperationMessage)
    
    [<Theory; AutoMockIntegrationData>]
    let ``Document should exist when updating with a timestamp of 1`` (indexService : IIndexService, 
                                                                       documentService : IDocumentService, index : Index) = 
        indexService.AddIndex(index) |> ExpectSuccess
        let document = new FlexDocument(index.IndexName, "1")
        documentService.AddDocument(document) |> ExpectSuccess
        indexService.Refresh(index.IndexName) |> ExpectSuccess
        document.TimeStamp <- 1L
        documentService.AddOrUpdateDocument(document) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``Cannot create a document using update operation with a timestamp of 1`` (indexService : IIndexService, 
                                                                                   documentService : IDocumentService, 
                                                                                   index : Index) = 
        indexService.AddIndex(index) |> ExpectSuccess
        let document = new FlexDocument(index.IndexName, "1")
        document.TimeStamp <- 1L
        documentService.AddOrUpdateDocument(document) 
        |> ExpectErrorCode(Errors.INDEXING_DOCUMENT_ID_NOT_FOUND |> GenerateOperationMessage)
