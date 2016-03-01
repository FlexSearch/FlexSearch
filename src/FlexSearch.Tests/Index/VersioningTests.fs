namespace FlexSearch.Tests.Index

open FlexSearch.Tests
open FlexSearch.Core
open Swensen.Unquote

type VersioningTests() = 
    
    let versionTestFail id modifyIndex (reason) (ih : IntegrationHelper) = 
        let doc = createDocument id ih.IndexName |> withModifyIndex modifyIndex
        ih
        |> addDocument doc
        |> testFail reason
    
    member __.``Cannot create a duplicate document with a modifyIndex of -1`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> versionTestFail "1" -1L (DocumentIdAlreadyExists(ih.IndexName, "1"))
    
    member __.``Cannot create a duplicate document with modifyIndex of -1 even after cache is cleared`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> refreshIndexPass
        ih |> versionTestFail "1" -1L (DocumentIdAlreadyExists(ih.IndexName, "1"))
    
    member __.``Cannot create a document with modifyIndex of 1`` (ih : IntegrationHelper) = 
        // Modify index of 1 implies that we want to ensure that the document exists 
        // which is against the logic of basic create operation
        ih |> addIndexPass
        ih |> versionTestFail "1" 1L (IndexingVersionConflict(ih.IndexName, "1", "1"))
    
    member __.``Duplicate document can be created with a modifyIndex of 0`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> totalDocs 0
        ih |> addDocByIdPass "1"
        ih |> addDocByIdPass "1"
        ih |> totalDocs 2
    
    member __.``For optimistic update the modifyIndex should match`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        let doc = createDocument "1" ih.IndexName |> withModifyIndex 2L
        ih
        |> addOrUpdateDocument doc
        |> testSuccess
    
    member __.``Cannot update a document with wrong modifyIndex`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        let doc = createDocument "1" ih.IndexName |> withModifyIndex 100L
        ih
        |> addOrUpdateDocument doc
        |> testFail (IndexingVersionConflict(ih.IndexName, "1", "2"))
    
    member __.``Document should exist when updating with a modifyIndex of 1`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        let doc = createDocument "1" ih.IndexName |> withModifyIndex 1L
        ih
        |> addOrUpdateDocument doc
        |> testSuccess
    
    member __.``Cannot create a document using update operation with a modifyIndex of 1`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        let doc = createDocument "1" ih.IndexName |> withModifyIndex 1L
        ih
        |> addOrUpdateDocument doc
        |> testFail (DocumentIdNotFound(ih.IndexName, "1"))
    
    member __.``A newly created document should have modifyIndex greater than 1`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> refreshIndexPass
        test <@ (ih |> getDocExt "1").ModifyIndex > 1L @>
    
    member __.``ModifyIndex field can be correctly retieved from the physical medium`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> commitIndexPass
        ih |> closeIndexPass
        ih |> openIndexPass
        test <@ (ih |> getDocExt "1").ModifyIndex > 1L @>
    
    //    member __.``Fields returned by the document service should match the total number of fields in the index`` (indexService : IIndexService, 
    //                                                                                                                documentService : IDocumentService, 
    //                                                                                                                index : Index) = 
    //        test <@ succeeded <| indexService.AddIndex(index) @>
    //        let document = new Document(indexName = index.IndexName, id = "1")
    //        test <@ succeeded <| documentService.AddDocument(document) @>
    //        test <@ succeeded <| indexService.Refresh(index.IndexName) @>
    //        test <@ succeeded <| indexService.Commit(index.IndexName) @>
    //        test <@ succeeded <| indexService.CloseIndex(index.IndexName) @>
    //        test <@ succeeded <| indexService.OpenIndex(index.IndexName) @>
    //        test 
    //            <@ (extract <| documentService.GetDocument(index.IndexName, document.Id)).Fields.Count = index.Fields.Length @>
    //    
    member __.``Version Cache gets cleared after a refresh is called`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> refreshIndexPass
        let indexWriter = extract <| ih.IndexService.IsIndexOnline(ih.IndexName)
        test <@ indexWriter.Caches.[0].Current.Count = 0 @>
    
    member __.``Document version can be reterieved even after all caches are cleared - 1`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> refreshIndexPass
        let indexWriter = extract <| ih.IndexService.IsIndexOnline(ih.IndexName)
        indexWriter.Caches.[0].Current.Clear()
        indexWriter.Caches.[0].Old.Clear()
        // As all the caches are cleared the index has to load the document version
        // from the doc values. For optimistic update to work the modifyIndex has 
        // to match
        let doc = createDocument "1" ih.IndexName |> withModifyIndex 2L
        ih
        |> addOrUpdateDocument doc
        |> testSuccess

    member __.``Document version can be reterieved even after all caches are cleared - 2`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> refreshIndexPass
        let indexWriter = extract <| ih.IndexService.IsIndexOnline(ih.IndexName)
        indexWriter.Caches.[0].Current.Clear()
        indexWriter.Caches.[0].Old.Clear()
        let doc = createDocument "1" ih.IndexName |> withModifyIndex 5L
        ih
        |> addOrUpdateDocument doc
        |> testFail (IndexingVersionConflict(ih.IndexName, "1", "2"))
    
    member __.``The modifyIndex will auto increment``(ih : IntegrationHelper) =
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> addDocByIdPass "2"
        ih |> addDocByIdPass "3"
        ih |> refreshIndexPass
        test <@ (ih |> getDocExt "1").ModifyIndex = 2L @>
        test <@ (ih |> getDocExt "2").ModifyIndex = 3L @>
        test <@ (ih |> getDocExt "3").ModifyIndex = 4L @>

    member __.``After reopening modifyIndex will continue from last highest modifedIndex value``(ih : IntegrationHelper) =
        __.``The modifyIndex will auto increment``(ih)
        ih |> commitIndexPass
        ih |> closeIndexPass
        ih |> openIndexPass
        ih |> addDocByIdPass "4"
        ih |> refreshIndexPass
        test <@ (ih |> getDocExt "4").ModifyIndex = 5L @>

    member __.``Versioning information is loaded from cache till refresh is called``(ih : IntegrationHelper) =
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        let indexWriter = extract <| ih.IndexService.IsIndexOnline(ih.IndexName)
        test <@ indexWriter.Caches.[0].Current.Count = 1 @>
        let doc = createDocument "1" ih.IndexName |> withModifyIndex 5L
        ih
        |> addOrUpdateDocument doc
        |> testFail (IndexingVersionConflict(ih.IndexName, "1", "2"))
        