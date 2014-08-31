namespace FlexSearch.Api.Messages

open FlexSearch.Api
open System.Collections.Generic

type Response<'T> () =
    member val Data = Unchecked.defaultof<'T> with get, set
    member val Error = Unchecked.defaultof<OperationMessage> with get, set

type CreateResponse() =
    member val Id = Unchecked.defaultof<string> with get, set
     
type IndexStatusResponse() = 
    member val Status = Unchecked.defaultof<IndexState> with get, set

type IndexExistsResponse() = 
    member val Exists = Unchecked.defaultof<bool> with get, set
