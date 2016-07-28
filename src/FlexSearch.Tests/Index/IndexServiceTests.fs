namespace FlexSearch.Tests.Index

open FlexSearch.Tests
open FlexSearch.Core

type AddIndexTests() = 
    member __.``Should add a new index`` (ih : IntegrationHelper) = ih |> addIndexPass
    
    member __.``Newly created index should be online`` (ih : IntegrationHelper) = 
        ih.Index.Active <- true
        ih |> addIndexPass
        ih |> testIndexOnline
    
    member __.``Newly created index should be offline`` (ih : IntegrationHelper) = 
        ih.Index.Active <- false
        ih |> addIndexPass
        ih |> testIndexOffline
    
    member __.``It is not possible to open an opened index`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> testIndexOnline
        ih |> openIndexFail
    
    member __.``It is not possible to close a closed index`` (ih : IntegrationHelper) = 
        ih.Index.Active <- false
        ih |> addIndexPass
        ih |> testIndexOffline
        ih |> closeIndexFail
    
    member __.``Can not create the same index twice`` (ih : IntegrationHelper) = 
        ih |> addIndexPass
        ih |> addIndexFail (IndexAlreadyExists(ih.Index.IndexName))
    
    member __.``Offline index can be made online`` (ih : IntegrationHelper) = 
        ih.Index.Active <- false
        ih |> addIndexPass
        ih |> openIndexPass
        ih |> testIndexOnline
    
    member __.``Online index can be made offline`` (ih : IntegrationHelper) = 
        ih.Index.Active <- true
        ih |> addIndexPass
        ih |> closeIndexPass
        ih |> testIndexOffline

    member __.``Adding an index with erroneous settings should keep the index closed`` (ih : IntegrationHelper) =
        ih.Index.Active <- true
        // Duplicate field names => error
        ih.Index.Fields <- [| ih.Index.Fields.[1] |] |> Array.append ih.Index.Fields
        ih.IndexService.AddIndex ih.Index |> hasFailed
        ih |> testIndexOffline

    member __.``Updating an already closed index should be successful`` (ih : IntegrationHelper) =
        ih.Index.Active <- false
        ih |> addIndexPass
        ih.Index.IndexConfiguration.CommitTimeSeconds <- 50
        ih.IndexService.UpdateIndexConfiguration (ih.IndexName, ih.Index.IndexConfiguration) |> succeeded

    member __.``Adding or updating a predefined query shouldn't keep the index closed`` (ih : IntegrationHelper) =
        ih.Index.Active <- true
        ih |> addIndexPass
        let pq = getQuery(ih.IndexName, "allof(fieldName with spaces, '')")
                 |> withPredefinedQuery "test"
        ih.IndexService.AddOrUpdatePredefinedQuery(ih.IndexName, pq) |> hasFailed
        ih |> testIndexOnline

