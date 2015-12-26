// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch.Api.Models
open MsgPack.Serialization
open System
open System.IO

module TransactionLog = 
    [<Internal>]
    type Operation = 
        | Create = 1
        | Update = 2
        | Delete = 3
    
    /// Represents a single Transaction log record entry.
    [<CLIMutableAttribute>]
    type T = 
        { TransactionId : int64
          Operation : Operation
          Document : Document
          /// This will be used for delete operation as we
          /// don't require a document
          Id : string
          /// This will be used for delete operation
          Query : string }
        
        static member Create(tranxId, operation, document, ?id : string, ?query : string) = 
            let id = defaultArg id String.Empty
            let query = defaultArg query String.Empty
            { TransactionId = tranxId
              Operation = operation
              Document = document
              Id = id
              Query = query }
        
        static member Create(txId, id) = 
            { TransactionId = txId
              Operation = Operation.Delete
              Document = Unchecked.defaultof<Document>
              Id = id
              Query = String.Empty }
    
    let msgPackSerializer = SerializationContext.Default.GetSerializer<T>()
    let serializer (stream, entry : T) = msgPackSerializer.Pack(stream, entry)
    let deSerializer (stream) = msgPackSerializer.Unpack(stream)
    
    type TxWriter(path : string, gen : int64) = 
        inherit SingleConsumerQueue<byte [] * int64>()
        let mutable currentGen = gen
        let mutable fileStream = Unchecked.defaultof<_>
        let populateFS() = 
            fileStream <- new FileStream(path +/ currentGen.ToString(), FileMode.Append, FileAccess.Write, 
                                         FileShare.ReadWrite, 1024, true)
        do populateFS()
        
        override __.Process(item : byte [] * int64) = 
            let (data, gen) = item
            if gen <> currentGen then 
                fileStream.Close()
                currentGen <- gen
                populateFS()
            await <| fileStream.WriteAsync(data, 0, data.Length)
            fileStream.Flush()
        
        /// Reads existing Transaction Log and returns all the entries
        member __.ReadLog(gen : int64) = 
            if File.Exists(path +/ gen.ToString()) then 
                try 
                    seq { 
                        use fileStream = 
                            new FileStream(path +/ gen.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                        while fileStream.Position <> fileStream.Length do
                            yield msgPackSerializer.Unpack(fileStream)
                    }
                with e -> 
                    Logger.Log <| TransactionLogReadFailure(path +/ gen.ToString(), exceptionPrinter e)
                    Seq.empty
            else Seq.empty
        
        /// Append a new entry to TxLog
        member this.Append(data, gen) = this.Post(data, gen) |> ignore
        
        interface IDisposable with
            member __.Dispose() : unit = 
                if not (isNull fileStream) then fileStream.Close()
