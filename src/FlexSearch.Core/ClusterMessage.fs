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

module Cluster =
    open ProtoBuf
    open System.IO
    open System.Collections.Generic
    open System
    open System.Linq
    open FlexSearch.Api
    open System.Reactive.Linq
    open System.Reactive
    open FlexSearch.Core.State
    /// Cluster messages 
    type MessageType =
        | AddIndexToCluster = 01uy
        | DeleteIndexFromCluster = 02uy
        | UpdateIndex = 03uy
        | GetIndexFrom = 04uy
        | UpdateAlias = 05uy
        | UpdateConf = 06uy
        | AddReplica = 07uy
        | UpdateReplica = 08uy
        | DeleteReplica = 09uy
        | MoveReplica = 10uy
        | AddReplicaFrom = 11uy

        | AddDocument = 21uy
        | DeleteDocument = 22uy
        | UpdateDocument = 23uy
        | GetDocument  = 24uy
        | AddToReplica = 25uy
        | DeleteFromReplica = 26uy
        | UpdateToReplica = 27uy

        | PurgeTLog = 41uy
        | GetTLog = 42uy

        | NodeLeave = 51uy
        | NodeJoin = 52uy

    let sendUpdateToAllNodes (message: byte[]) (state : NodeState) =
        let send = state.OutgoingConnections.Values.ToObservable().Do(fun x -> x.Send(message)).Materialize()
        let subscribe = send.Subscribe(fun x -> 
            match x.Kind with
            | NotificationKind.OnError -> ()
            |_ -> ())
        subscribe.Dispose()


    let addIndex (index: Index) (state: NodeState) = 
        let settings = ServiceLocator.SettingsBuilder.BuildSetting(index)
        ()
        //state.Indices.TryAdd(index.IndexName, index)
        

//    /// Utility method to send a message to the client using websocket
//    let SendMessage (message : 'a) (messageType: MessageType) (client: string) (state : NodeState) =
//        let msg = Array.append [|byte messageType|] (serialize message)
//
//        match state.OutgoingConnections.TryGetValue(client) with
//        | (true, x) -> 
//            if x.Connected() then
//                x.Send(msg)
//            else
//                failwithf "The client is not connected."
//        | _ -> failwithf "No connection exists to the specifiec client: %s." client
//
//
//    /// Utility method to process the incoming message
//    let ProcessMessage (message : byte[]) (state : NodeState) =  
//        if message.Length = 0 then
//            failwith "Empty message cannot be processed."
//        
//        let msgType: MessageType = LanguagePrimitives.EnumOfValue message.[0]
//        let body = message.Skip(1).ToArray()
//
//        match message.[0] with
//        | 1uy -> deSerialize body
//        | _ -> failwithf "The message type is not supported."

