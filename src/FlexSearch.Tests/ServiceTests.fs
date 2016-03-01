namespace FlexSearch.Tests.ServiceTests
open FlexSearch.Tests
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open FlexSearch.Core
open Swensen.Unquote

module IndexServiceTests = 
    
    type UpdateIndexTests() =
        let modifyFirstField (index : Index) =
            let fields = index.Fields
            fields.[0].AllowSort <- false
            index

        let modifyFirstFieldName (index : Index) =
            let fields = index.Fields
            fields.[0].FieldName <- "modified"
            index

        let searchAndExtract (searchService : ISearchService) query = 
            let result = searchService.Search(query)
            test <@ succeeded <| result @>
            extract result

        let setUpAndModifyProfile (index : Index) indexService documentService searchService newProfile =
            let testData = """
id,et1,et2,i1,i2
1,a,h,37,95
2,b,g,49,31"""
            index.PredefinedQueries <- [| new SearchQuery(index.IndexName, "allof(et1, 'a')", QueryName = "profile") |]
            indexTestData (testData, index, indexService, documentService)

            let result = new SearchQuery(index.IndexName, "", PredefinedQuery = "profile")
                         |> searchAndExtract searchService
            test <@ result.TotalAvailable = 1 @>
            test <@ result.Documents.[0].Id = "1" @>

            indexService.AddOrUpdatePredefinedQuery(index.IndexName, newProfile) |> (?)
    
        

        member __.``Should allow updating index fields`` (indexService : IIndexService, index : Index) =
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            
            let modified = index |> modifyFirstField
            test <@ succeeded <| indexService.UpdateIndexFields(index.IndexName, modified.Fields) @>
            
            let updated = indexService.GetIndex index.IndexName
            test <@ succeeded updated @>
            test <@ (extract updated).Fields.[0].AllowSort = false @>

        member __.``Should be able to access old documents after updating index fields``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService) =
            index.Active <- true
            indexService.AddIndex(index) |> (?)
            documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) |> (?)

            let modified = index |> modifyFirstField
            indexService.UpdateIndexFields(index.IndexName, modified.Fields) |> (?)
            
            documentService.GetDocument(index.IndexName, "1") |> (?)

        member __.``Should be able to access old documents after changing index field name``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService) =
            index.Active <- true
            indexService.AddIndex(index) |> (?)
            documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) |> (?)

            let modified = index |> modifyFirstFieldName
            indexService.UpdateIndexFields(index.IndexName, modified.Fields) |> (?)
            
            let doc = documentService.GetDocument(indexName = index.IndexName, id = "1") 
            (?) doc
            test <@ (extract doc).Fields.ContainsKey("modified") @>

        member __.``Should be able to access old documents after modifying search profile``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService,
                    searchService : ISearchService) =
            new SearchQuery(index.IndexName, "allof(et1, 'b')", QueryName = "profile") 
            |> setUpAndModifyProfile index indexService documentService searchService
            
            // Search using the new profile and check that the second record is returned
            let result = new SearchQuery(index.IndexName, "", PredefinedQuery = "profile")
                         |> searchAndExtract searchService
            test <@ result.TotalAvailable = 1 @>
            test <@ result.Documents.[0].Id = "2" @>

        member __.``Should be able to access old documents after adding new search profile``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService,
                    searchService : ISearchService) =
            new SearchQuery(index.IndexName, "allof(et1, 'b')", QueryName = "profile2") 
            |> setUpAndModifyProfile index indexService documentService searchService
            
            // Search using the new profile and check that the second record is returned
            let result = new SearchQuery(index.IndexName, "", PredefinedQuery = "profile2")
                         |> searchAndExtract searchService
            test <@ result.TotalAvailable = 1 @>
            test <@ result.Documents.[0].Id = "2" @>

        member __.``Should be able to access old documents after changing index configuration``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService,
                    searchService : ISearchService) =
            index.Active <- true
            indexService.AddIndex(index) |> (?)
            documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) |> (?)

            let conf = index.IndexConfiguration
            conf.AutoCommit <- true
            indexService.UpdateIndexConfiguration(index.IndexName, conf) |> (?)
            
            documentService.GetDocument(index.IndexName, "1") |> (?)

        member __.``Shouldn't be able modify Index Version``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService,
                    searchService : ISearchService) =
            index.Active <- true
            indexService.AddIndex(index) |> (?)
            documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) |> (?)

            let conf = new IndexConfiguration()
            conf.IndexVersion <- IndexVersion.FlexSearch_1B
            test <@ failed <| indexService.UpdateIndexConfiguration(index.IndexName, conf) @>

    type CommonTests() =
        member __.``Should return size of existing index`` (indexService: IIndexService, index : Index) =
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ succeeded <| indexService.Commit(index.IndexName) @>
            test <@ succeeded <| indexService.GetDiskUsage index.IndexName @>

        member __.``Should fail when asking for size of non-existing index`` (indexService: IIndexService) =
            test <@ failed <| indexService.GetDiskUsage "non-existing-index" @>

module DocumentServiceTests = 
    type DocumentManagementTests() = 
        
        member __.``Should be able to add and retrieve simple document`` (index : Index, documentId : string, 
                                                                          indexService : IIndexService, 
                                                                          documentService : IDocumentService) = 
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = documentId)
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            test <@ documentService.TotalDocumentCount(index.IndexName) = ok(1) @>
            test <@ (extract <| documentService.GetDocument(index.IndexName, documentId)).Id = documentId @>
        
        member __.``Should be able to add and retrieve document after closing the index`` (index : Index, 
                                                                                           documentId : string, 
                                                                                           indexService : IIndexService, 
                                                                                           documentService : IDocumentService) = 
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = documentId)
            document.Fields.Add("et1", "test")
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Commit(index.IndexName) @>
            test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
            test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
            test <@ documentService.TotalDocumentCount(index.IndexName) = ok(1) @>
            let doc = documentService.GetDocument(index.IndexName, documentId)
            test <@ (extract <| doc).Id = documentId @>
            test <@ (extract <| doc).Fields.["et1"] = "test" @>
        
        member __.``Should be able to add and delete a document`` (index : Index, documentId : string, 
                                                                   indexService : IIndexService, 
                                                                   documentService : IDocumentService) = 
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = documentId)
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            test <@ documentService.TotalDocumentCount(index.IndexName) = ok(1) @>
            test <@ (extract <| documentService.GetDocument(index.IndexName, documentId)).Id = documentId @>
            test <@ succeeded <| documentService.DeleteDocument(document.IndexName, documentId) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 0 @>
            test <@ failed <| documentService.GetDocument(index.IndexName, documentId) @>
        
        member __.``Should be able to update a document`` (index : Index, id : string, indexService : IIndexService, 
                                                           documentService : IDocumentService) = 
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = id)
            document.Fields.["t1"] <- "0"
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            test <@ (extract <| documentService.GetDocument(index.IndexName, id)).Fields.["t1"] = "0" @>
            // Update the document
            document.Fields.["t1"] <- "1"
            document.TimeStamp <- 0L
            test <@ succeeded <| documentService.AddOrUpdateDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            test <@ (extract <| documentService.GetDocument(index.IndexName, id)).Fields.["t1"] = "1" @>

        member __.``Should be able to delete all documents in an index``(index : Index, indexService : IIndexService, 
                                                                         documentService : IDocumentService) =
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            [1..10] |> Seq.iter (fun i ->
                let d = new Document(indexName = index.IndexName, id = i.ToString())
                d.Fields.["t1"] <- "0"
                test <@ succeeded <| documentService.AddDocument(d) @>)

            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            
            // Initially we have 10 docs
            test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 10 @>

            // After deletion we have 0 docs
            test <@ succeeded <| documentService.DeleteAllDocuments(index.IndexName) @>
            test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 0 @>

        member __.``Should be able to delete documents returned by search query``(index: Index, indexService: IIndexService,
                                                                                  documentService: IDocumentService, searchService: ISearchService) =
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            [1..10] |> Seq.iter (fun i ->
                let d = new Document(indexName = index.IndexName, id = i.ToString())
                d.Fields.["i1"] <- i.ToString()
                test <@ succeeded <| documentService.AddDocument(d) @>)

            test <@ succeeded <| indexService.Refresh(index.IndexName) @>

            // Initially we have 10 docs
            test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 10 @>

            // Create a query that brings back 4 docs
            let query = new SearchQuery(index.IndexName, "le(i1, '4')")
            let searchRes = searchService.Search(query)
            test <@ succeeded searchRes @>
            test <@ (extract searchRes).RecordsReturned = 4 @>

            // Execute deletion query 
            let delResult = documentService.DeleteDocumentsFromSearch(index.IndexName, query)
            test <@ succeeded delResult @>
            test <@ (extract delResult).RecordsReturned = 4 @>

            test <@ succeeded <| indexService.Refresh index.IndexName @>

            // Now we should only have 6 docs left
            test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 6 @>

type QueueServiceTests() = 
    member __.``Queue service can be used to add document to an index`` (indexService : IIndexService, 
                                                                         queueService : IQueueService, 
                                                                         documentService : IDocumentService, 
                                                                         index : Index) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        let document = new Document(indexName = index.IndexName, id = "1")
        let q = new QueueService(documentService) :> IQueueService
        queueService.AddDocumentQueue(document)
        // Wait for the action block to actually finish adding the document to Lucene
        queueService.Complete()
        test <@ succeeded <| indexService.Refresh(index.IndexName) @>
        test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 1 @>
        
