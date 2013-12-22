// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

module HttpModule = 

    open System.Net
    open FlexSearch.Core
    open FlexSearch.Core.Cluster
    open FlexSearch.Api
    open Newtonsoft.Json
    open System.ComponentModel.Composition
    open System.Collections.Generic
    open System.Linq
    open FlexSearch.Core.HttpHelpers
    open System.Net.Http

//    let sendUpdateToAllNodes (message: ByteArrayContent) (state : NodeState) =
//        let results =
//            state.HttpConnections.Values.ToArray()
//            |> Array.map(fun x-> Async.AwaitTask(x.PostAsync("", message))
//            |> Async.Parallel
//            |> Async.RunSynchronously
        
        

    [<Export(typeof<IHttpModule>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "indices")>]
    type IndexModule() =
        interface IHttpModule with
            member this.Process (request: System.Net.HttpListenerRequest) (response: System.Net.HttpListenerResponse) (state: NodeState) = 
                match request with         
                // /indices/status
                | POST "state" _ -> ()
                    
                              
                // /indices/{indexname}
                | GET "*" x -> 
                    let index = state.PersistanceStore.Get<Index>(x)
                    OK index request response  
                 
                // /indices
                | POST "*" x -> 
                    match state.IndexExists(x) with
                    | Some(x) -> 
                        BAD_REQUEST indexAlreadyExist request response
                    | None -> 
                        match HttpHelpers.getRequestBody<Index>(request) with
                        | Choice1Of2(body) -> addIndex body state |> ignore
                        | Choice2Of2(error) -> BAD_REQUEST error request response 
                        


                // /indices/{indexname}
                | PUT "*" x -> ()

                // /indices/{indexname}
                | DELETE "*" x -> ()

                | _ -> ()