module ServiceTests

open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open FlexSearch.Core
open Swensen.Unquote

module IndexServiceTests = 
    type AddIndexTests() = 
        member __.``Should add a new index`` (index : Index, indexService : IIndexService) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
        
        member __.``Newly created index should be online`` (indexService : IIndexService, index : Index) = 
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.GetIndexState(index.IndexName) = ok(IndexStatus.Online) @>
        
        member __.``Newly created index should be offline`` (indexService : IIndexService, index : Index) = 
            index.Active <- false
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.GetIndexState(index.IndexName) = ok(IndexStatus.Offline) @>
        
        member __.``It is not possible to open an opened index`` (indexService : IIndexService, index : Index) = 
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.OpenIndex(index.IndexName) = fail(IndexIsAlreadyOnline(index.IndexName)) @>
        
        member __.``It is not possible to close an closed index`` (indexService : IIndexService, index : Index) = 
            index.Active <- false
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.CloseIndex(index.IndexName) = fail(IndexIsAlreadyOffline(index.IndexName)) @>
        
        member __.``Can not create the same index twice`` (indexService : IIndexService, index : Index) = 
            index.Active <- false
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ indexService.AddIndex(index) = fail(IndexAlreadyExists(index.IndexName)) @>
        
        member __.``Offline index can be made online`` (indexService : IIndexService, index : Index) = 
            index.Active <- false
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
            test <@ indexService.GetIndexState(index.IndexName) = ok(IndexStatus.Online) @>
        
        member __.``Online index can be made offline`` (indexService : IIndexService, index : Index) = 
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
            test <@ indexService.GetIndexState(index.IndexName) = ok(IndexStatus.Offline) @>

    type UpdateIndexTests() =
        let modifyFirstField (index : Index) =
            let fields = index.Fields
            fields.[0].Store <- false
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
            index.SearchProfiles <- [| new SearchQuery(index.IndexName, "et1 = 'a'", QueryName = "profile") |]
            indexTestData (testData, index, indexService, documentService)

            let result = new SearchQuery(index.IndexName, "", SearchProfile = "profile")
                         |> searchAndExtract searchService
            test <@ result.TotalAvailable = 1 @>
            test <@ result.Documents.[0].Id = "1" @>

            indexService.AddOrUpdateSearchProfile(index.IndexName, newProfile) |> (?)
    
        

        member __.``Should allow updating index fields`` (indexService : IIndexService, index : Index) =
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            
            let modified = index |> modifyFirstField
            test <@ succeeded <| indexService.UpdateIndexFields(index.IndexName, modified.Fields) @>
            
            let updated = indexService.GetIndex index.IndexName
            test <@ succeeded updated @>
            test <@ (extract updated).Fields.[0].Store = false @>

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
            new SearchQuery(index.IndexName, "et1 = 'b'", QueryName = "profile") 
            |> setUpAndModifyProfile index indexService documentService searchService
            
            // Search using the new profile and check that the second record is returned
            let result = new SearchQuery(index.IndexName, "", SearchProfile = "profile")
                         |> searchAndExtract searchService
            test <@ result.TotalAvailable = 1 @>
            test <@ result.Documents.[0].Id = "2" @>

        member __.``Should be able to access old documents after adding new search profile``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService,
                    searchService : ISearchService) =
            new SearchQuery(index.IndexName, "et1 = 'b'", QueryName = "profile2") 
            |> setUpAndModifyProfile index indexService documentService searchService
            
            // Search using the new profile and check that the second record is returned
            let result = new SearchQuery(index.IndexName, "", SearchProfile = "profile2")
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
            conf.IndexVersion <- IndexVersion.Lucene_4_x_x
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
            let query = new SearchQuery(index.IndexName, "i1 <= '4'")
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

    
    type ``Versioning tests``() = 
        
        member __.``Cannot create a duplicate document with a timestamp of -1`` (indexService : IIndexService, 
                                                                                 documentService : IDocumentService, 
                                                                                 index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            document.TimeStamp <- -1L
            test <@ documentService.AddDocument(document) = fail(DocumentIdAlreadyExists(index.IndexName, "1")) @>
        
        member __.``Cannot create a duplicate document with a timestamp of -1 even after cache is cleared`` (indexService : IIndexService, 
                                                                                                             documentService : IDocumentService, 
                                                                                                             index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            document.TimeStamp <- -1L
            test <@ documentService.AddDocument(document) = fail(DocumentIdAlreadyExists(index.IndexName, "1")) @>
        
        member __.``Cannot create a document with timestamp of 1`` (indexService : IIndexService, 
                                                                    documentService : IDocumentService, 
                                                                    index : Index) = 
            // TimeStamp of 1 implies that we want to ensure that the document exists which is against the logic of basic create operation
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1", TimeStamp = 1L)
            test <@ failed <| documentService.AddDocument(document) @>
        
        member __.``Duplicate document can be created with a timestamp of 0`` (indexService : IIndexService, 
                                                                               documentService : IDocumentService, 
                                                                               index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1", TimeStamp = 0L)
            test <@ succeeded <| documentService.AddDocument(document) @>
            document.TimeStamp <- 0L
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 2 @>
        
        member __.``For optimistic update the timestamp should match`` (indexService : IIndexService, 
                                                                        documentService : IDocumentService, 
                                                                        index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            test <@ succeeded <| documentService.AddOrUpdateDocument(document) @>
            document.TimeStamp <- 1000L
            test <@ failed <| documentService.AddOrUpdateDocument(document) @>
        
        member __.``Cannot update a document with wrong timestamp`` (indexService : IIndexService, 
                                                                     documentService : IDocumentService, 
                                                                     index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            document.TimeStamp <- 1000L
            test <@ failed <| documentService.AddOrUpdateDocument(document) @>
        
        member __.``Document should exist when updating with a timestamp of 1`` (indexService : IIndexService, 
                                                                                 documentService : IDocumentService, 
                                                                                 index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            document.TimeStamp <- 1L
            test <@ succeeded <| documentService.AddOrUpdateDocument(document) @>
        
        member __.``Cannot create a document using update operation with a timestamp of 1`` (indexService : IIndexService, 
                                                                                             documentService : IDocumentService, 
                                                                                             index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1", TimeStamp = 1L)
            test 
                <@ documentService.AddOrUpdateDocument(document) = fail
                                                                       (DocumentIdNotFound(index.IndexName, document.Id)) @>
        
        member __.``A newly created document should have version number greater than 1`` (indexService : IIndexService, 
                                                                                          documentService : IDocumentService, 
                                                                                          index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            test <@ (extract <| documentService.GetDocument(index.IndexName, document.Id)).TimeStamp > 1L @>
        
        member __.``Timestamp field can be correctly retieved from the physical medium`` (indexService : IIndexService, 
                                                                                          documentService : IDocumentService, 
                                                                                          index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Commit(index.IndexName) @>
            test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
            test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
            test <@ (extract <| documentService.GetDocument(index.IndexName, document.Id)).TimeStamp > 1L @>
        
        member __.``Fields returned by the document service should match the total number of fields in the index`` (indexService : IIndexService, 
                                                                                                                    documentService : IDocumentService, 
                                                                                                                    index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
            test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
            test 
                <@ (extract <| documentService.GetDocument(index.IndexName, document.Id)).Fields.Count = index.Fields.Length @>
        
        member __.``Version Cache gets cleared after a refresh is called`` (indexService : IIndexService, 
                                                                            documentService : IDocumentService, 
                                                                            index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            let indexWriter = extract <| indexService.IsIndexOnline(index.IndexName)
            test <@ indexWriter.Caches.[0].Current.Count = 0 @>
        
        member __.``Document version can be reterieved even after all caches are cleared`` (indexService : IIndexService, 
                                                                                            documentService : IDocumentService, 
                                                                                            index : Index) = 
            test <@ succeeded <| indexService.AddIndex(index) @>
            let document = new Document(indexName = index.IndexName, id = "1")
            test <@ succeeded <| documentService.AddDocument(document) @>
            test <@ succeeded <| indexService.Refresh(index.IndexName) @>
            let indexWriter = extract <| indexService.IsIndexOnline(index.IndexName)
            indexWriter.Caches.[0].Current.Clear()
            indexWriter.Caches.[0].Old.Clear()
            // As all the caches are cleared the index has to load the documment version from the
            // docvalues. For optimistic update to work the timestamp has to match 
            test <@ succeeded <| documentService.AddOrUpdateDocument(document) @>

type QueueServiceTests() = 
    member __.``Queue service can be used to add document to an index`` (indexService : IIndexService, 
                                                                         queueService : IQueueService, 
                                                                         documentService : IDocumentService, 
                                                                         index : Index) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        let document = new Document(indexName = index.IndexName, id = "1")
        let q = new QueueService(documentService) :> IQueueService
        // TODO Use Queues.fs for the QueueService; Expose Queue object so that we can shut it down
        queueService.AddDocumentQueue(document)
        // TODO wait for the action block to actually finish adding the document to Lucene
        test <@ succeeded <| indexService.Refresh(index.IndexName) @>
        test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = 1 @>
        
