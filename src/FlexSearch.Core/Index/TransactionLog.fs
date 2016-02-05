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

open FlexSearch.Api.Model
open MsgPack.Serialization
open System
open System.IO
open System.Security.AccessControl
open System.Text
open System.Collections.Generic

[<Internal>]
type TxOperation = 
    | Create = 1
    | Update = 2
    | Delete = 3

/// Represents a single Transaction log record entry.
[<CLIMutableAttribute>]
type TransactionEntry = 
    { ModifyIndex : int64
      Operation : TxOperation
      Data : Fields
      Id : string }
    
    static member Create(modifyIndex, operation, data, id : string) = 
        { ModifyIndex = modifyIndex
          Operation = operation
          Data = data
          Id = id }
    
    /// Used for creating delete entry
    static member Create(modifyIndex, id : string) = 
        { ModifyIndex = modifyIndex
          Operation = TxOperation.Delete
          Data = Unchecked.defaultof<_>
          Id = id }

    static member MsgPackSerializer = SerializationContext.Default.GetSerializer<TransactionEntry>()
    static member Serializer(stream, entry : TransactionEntry) = TransactionEntry.MsgPackSerializer.Pack(stream, entry)
    static member DeSerializer(stream) = TransactionEntry.MsgPackSerializer.Unpack(stream)

/// TxWriter is used for writing transaction entries to a text file using MessagePack serializer.
/// NOTE: This is not threadsafe and should be used as a pooled resource for performance.
type TxWriter(gen : int64, ?path0 : string) = 
    let path = defaultArg path0 (Path.GetTempFileName())
    let mutable currentGen = gen
    let mutable fileStream = Unchecked.defaultof<_>
    let populateFS() = 
        let localPath = if path0.IsSome then path +/ currentGen.ToString() else path
        fileStream <- new FileStream(localPath, FileMode.OpenOrCreate, FileSystemRights.AppendData, 
                                     FileShare.ReadWrite, 1024, FileOptions.Asynchronous)
    do populateFS()
    
    member __.AppendEntry(entry : TransactionEntry, gen : int64) = 
        if gen <> currentGen then 
            fileStream.Close()
            currentGen <- gen
            populateFS()
        use stream = Pools.memory.GetStream()
        TransactionEntry.Serializer(stream, entry)
        // Avoid using ToArray as it allocates a lot of memory
        fileStream.Write(stream.GetBuffer(), 0, int stream.Position)     
        fileStream.Flush()
    
    /// Reads existing Transaction Log and returns all the entries
    member __.ReadLog(gen : int64) = 
        let localPath = if path0.IsSome then path +/ gen.ToString() else path
        if File.Exists(localPath) then 
            try 
                seq { 
                    use fs = 
                        new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                    while fs.Position <> fs.Length do
                        yield TransactionEntry.MsgPackSerializer.Unpack(fs)
                }
            with e -> 
                Logger.Log <| TransactionLogReadFailure(path +/ gen.ToString(), exceptionPrinter e)
                Seq.empty
        else Seq.empty
        
    interface IDisposable with
        member __.Dispose() : unit = 
            if not (isNull fileStream) then fileStream.Close()
