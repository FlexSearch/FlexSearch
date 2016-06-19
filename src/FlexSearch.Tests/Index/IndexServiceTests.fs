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
