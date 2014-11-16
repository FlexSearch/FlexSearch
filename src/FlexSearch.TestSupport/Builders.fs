namespace FlexSearch.TestSupport
open FlexSearch.Api
open System
[<AutoOpen>]
module IndexBuilders =
    
    let CreateIndexWithName (indexName : string) =
        let index = new Index(IndexName = indexName)
        index

    let CreateIndexWithAutoName () =
        let index = new Index(IndexName = Guid.NewGuid().ToString("N"))
        index
