namespace FlexSearch.Tests

open FlexSearch.Tests

type ``Operator: 'allOf' Tests``(ih : IntegrationHelper) = 
    let testData = """
id,t1,t2,i1
1,Aaron,johnson,23
2,aaron,hewitt,32
3,Fred,Garner,44
4,aaron,Garner,43
5,fred,johnson,332"""
    do ih |> indexData testData
    member __.``Term match query supports array style syntax``() = ih |> verifyResultCount 2 "anyof(_id, '1','2')"
    member __.``Searching for 'id eq 1' should return 1 records``() = ih |> verifyResultCount 1 "allof(_id, '1')"
    member __.``Searching for int field 'i1 eq 44' should return 1 records``() = 
        ih |> verifyResultCount 1 "allof(i1, '44')"
    member __.``Searching for 'aaron' should return 3 records``() = ih |> verifyResultCount 3 "allof(t1, 'aaron')"
    member __.``Searching for 'aaron' & 'johnson' should return 1 record``() = 
        ih |> verifyResultCount 1 "allof(t1, 'aaron') and allof(t2, 'johnson')"
    member __.``Searching for t1 eq 'aaron' and (t2 eq 'johnson' or t2 eq 'Garner') should return 2 record``() = 
        ih |> verifyResultCount 2 "allof(t1, 'aaron') and (allof(t2, 'johnson') or allof(t2, 'Garner'))"
    member __.``Searching for 'id = 1' should return 1 records``() = ih |> verifyResultCount 1 "allof(_id, '1')"
    member __.``Searching for int field 'i1 = 44' should return 1 records``() = 
        ih |> verifyResultCount 1 "allof(i1, '44')"
    member __.``Searching for t1 = 'aaron' should return 3 records``() = ih |> verifyResultCount 3 "allof(t1, 'aaron')"
    member __.``Searching for t1 = 'aaron' and t2 = 'johnson' should return 1 record``() = 
        ih |> verifyResultCount 1 "allof(t1, 'aaron') and allof(t2, 'johnson')"
    member this.``Searching for t1 'aaron' & t2 'johnson or Garner' should return 2 record``() = 
        ih |> verifyResultCount 2 "allof(t1, 'aaron') and (allof(t2, 'johnson') or allof(t2, 'Garner'))"
