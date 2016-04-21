namespace FlexSearch.Tests.Index

open FlexSearch.Api.Model
open FlexSearch.Tests
open FlexSearch.Core
open Swensen.Unquote

type DocumentServiceTests() = 
    
    member __.``Should be able to add and retrieve simple document`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> refreshIndexPass
        ih |> totalDocs 1
        test <@ (ih |> getDocExt "1").Id = "1" @>
    
    member __.``Should be able to add and retrieve document after closing the index`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        let doc = createDocument "1" ih.IndexName |> withField "et1" "test"
        ih
        |> addDocument doc
        |> testSuccess
        ih |> commitIndexPass
        ih |> closeIndexPass
        ih |> openIndexPass
        ih |> totalDocs 1
        let result = ih |> getDocExt "1"
        test <@ result.Id = "1" @>
        test <@ result.Fields.["et1"] = "test" @>
    
    member __.``Should be able to add and delete a document`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> refreshIndexPass
        ih |> totalDocs 1
        test <@ (ih |> getDocExt "1").Id = "1" @>
        ih |> deleteDocByIdPass "1"
        ih |> refreshIndexPass
        ih |> totalDocs 0
        ih |> getDocsFail "1"
    
    member __.``Should be able to update a document`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        let doc = createDocument "1" ih.IndexName |> withField "t1" "0"
        ih
        |> addDocument doc
        |> testSuccess
        ih |> refreshIndexPass
        test <@ (ih |> getDocExt "1").Fields.["t1"] = "0" @>
        // Update the document
        let doc = createDocument "1" ih.IndexName |> withField "t1" "1"
        ih
        |> addOrUpdateDocument doc
        |> testSuccess
        ih |> refreshIndexPass
        test <@ (ih |> getDocExt "1").Fields.["t1"] = "1" @>
    
    member __.``Should be able to delete all documents in an index`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        [ 1..10 ] |> Seq.iter (fun i -> ih |> addDocByIdPass (i.ToString()))
        ih |> refreshIndexPass
        // Initially we have 10 docs
        ih |> totalDocs 10
        // After deletion we have 0 docs
        test <@ succeeded <| ih.DocumentService.DeleteAllDocuments(ih.IndexName) @>
        ih |> totalDocs 0
    
    member __.``Fields returned by the document service should match the total number of fields in the index`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addDocByIdPass "1"
        ih |> refreshIndexPass
        ih |> commitIndexPass
        ih |> closeIndexPass
        ih |> openIndexPass
        test <@ (extract <| ih.DocumentService.GetDocument(ih.IndexName, "1")).Fields.Count = ih.Index.Fields.Length @>
    
    //TODO: Delete by query needs further testing. Also we will have to enforce a commit every time
    // we delete by query    
    member __.``Should be able to delete documents returned by search query`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        [ 1..10 ] |> Seq.iter (fun i -> 
                         let doc = createDocument (i.ToString()) ih.IndexName |> withField "i1" (i.ToString())
                         ih
                         |> addDocument doc
                         |> testSuccess)
        ih |> refreshIndexPass
        // Initially we have 10 docs
        ih |> totalDocs 10
        // Create a query that brings back 4 docs
        let query = new SearchQuery(ih.IndexName, "le(i1, '4')")
        let searchRes = ih.SearchService.Search(query)
        test <@ succeeded searchRes @>
        test <@ (extract searchRes).RecordsReturned = 4 @>
        // Execute deletion query 
        let delResult = ih.DocumentService.DeleteDocumentsFromSearch(ih.IndexName, query)
        test <@ succeeded delResult @>
        test <@ (extract delResult).RecordsReturned = 4 @>
        ih |> refreshIndexPass
        // Now we should only have 6 docs left
        ih |> totalDocs 6

    member __.``Should be able to update a document with a very long key`` (ih : IntegrationHelper) = 
        let longKey = "l0ng5tr1ng_" |> String.replicate 100
        let document = new Document(longKey, ih.IndexName)
        document.Fields.Add("i1", "333")

        ih |> addIndexPass
        ih |> addDocument document |> testSuccess
        ih |> refreshIndexPass
        let docResp = ih.DocumentService.GetDocument(ih.IndexName, longKey)
        docResp |> testSuccess
        test<@ (extract docResp).Fields.["i1"] = "333" @>

        document.Fields.["i1"] <- "666"
        ih.DocumentService.AddOrUpdateDocument(document) |> testSuccess
        ih |> refreshIndexPass
        let updatedDocResp = ih.DocumentService.GetDocument(ih.IndexName, longKey)
        updatedDocResp |> testSuccess
        test<@ (extract updatedDocResp).Fields.["i1"] = "666" @>


        