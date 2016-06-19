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
            index.IndexConfiguration.CommitOnClose <- true
            indexTestData (testData, index, indexService, documentService)

            let result = new SearchQuery(index.IndexName, "", QueryName = "profile")
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
            index.IndexConfiguration.CommitOnClose <- true
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
            index.IndexConfiguration.CommitOnClose <- true
            indexService.AddIndex(index) |> (?)
            documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) |> (?)

            let modified = index |> modifyFirstFieldName
            indexService.UpdateIndexFields(index.IndexName, modified.Fields) |> (?)
            
            let doc = documentService.GetDocument(indexName = index.IndexName, id = "1") 
            (?) doc
            test <@ (extract doc).Fields.ContainsKey("modified") @>

        member __.``Should not be able to delete a field from an index``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService) =
            index.Active <- true
            index.IndexConfiguration.CommitOnClose <- true
            indexService.AddIndex(index) |> (?)
            documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) |> (?)

            let modified = index.Fields |> Array.skip 1
            test <@ indexService.UpdateIndexFields(index.IndexName, modified) |> failed @> 

        member __.``Should be able to access old documents after modifying predefined query``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService,
                    searchService : ISearchService) =
            new SearchQuery(index.IndexName, "allof(et1, 'b')", QueryName = "profile") 
            |> setUpAndModifyProfile index indexService documentService searchService
            
            // Search using the new profile and check that the second record is returned
            let result = new SearchQuery(index.IndexName, "", QueryName = "profile")
                         |> searchAndExtract searchService
            test <@ result.TotalAvailable = 1 @>
            test <@ result.Documents.[0].Id = "2" @>

        member __.``Should not be able to update an index to have 2 fields with the same name``
                  ( indexService: IIndexService,
                    index: Index ) =
            index.Active <- true
            indexService.AddIndex(index) |> (?)
            index.Fields <- [| index.Fields.[2] |] |> Array.append index.Fields
            indexService.UpdateIndexFields(index.IndexName, index.Fields)
            |> testFail (DuplicateFieldNamesNotAllowed(index.IndexName))

        member __.``Should be able to access old documents after adding new predefined query``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService,
                    searchService : ISearchService) =
            new SearchQuery(index.IndexName, "allof(et1, 'b')", QueryName = "profile2") 
            |> setUpAndModifyProfile index indexService documentService searchService
            
            // Search using the new profile and check that the second record is returned
            let result = new SearchQuery(index.IndexName, "", QueryName = "profile2")
                         |> searchAndExtract searchService
            test <@ result.TotalAvailable = 1 @>
            test <@ result.Documents.[0].Id = "2" @>

        member __.``Should not be able to add a predefined query without a name``
                ( indexService : IIndexService, 
                    index : Index) =
            index.Active <- true
            indexService.AddIndex index |> (?)
            let q = new SearchQuery(index.IndexName, "allof(et1, 'b')")
            test <@ indexService.AddOrUpdatePredefinedQuery(index.IndexName, q) |> failed @>
            indexService.DeleteIndex index.IndexName |> (?)

        member __.``Should be able to access old documents after changing index configuration``
                  ( indexService : IIndexService, 
                    index : Index,
                    documentService : IDocumentService,
                    searchService : ISearchService) =
            index.Active <- true
            index.IndexConfiguration.CommitOnClose <- true
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
            index.IndexConfiguration.CommitOnClose <- true
            indexService.AddIndex(index) |> (?)
            documentService.AddDocument(new Document(indexName = index.IndexName, id = "1")) |> (?)

            let conf = new IndexConfiguration()
            conf.IndexVersion <- IndexVersion.FlexSearch_1A
            test <@ failed <| indexService.UpdateIndexConfiguration(index.IndexName, conf) @>

    type CommonTests() =
        member __.``Should return size of existing index`` (indexService: IIndexService, index : Index) =
            index.Active <- true
            test <@ succeeded <| indexService.AddIndex(index) @>
            test <@ succeeded <| indexService.Commit(index.IndexName) @>
            test <@ succeeded <| indexService.GetDiskUsage index.IndexName @>

        member __.``Should fail when asking for size of non-existing index`` (indexService: IIndexService) =
            test <@ failed <| indexService.GetDiskUsage "non-existing-index" @>

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
        
