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
    static member DeSerializer(stream) = 
        try TransactionEntry.MsgPackSerializer.Unpack(stream) 
        with e -> 
            Logger.Log("Couldn't deserialize an entry from the transaction log", e, MessageKeyword.Default, MessageLevel.Warning)
            Unchecked.defaultof<TransactionEntry>

/// TxWriter is used for writing transaction entries to a text file using MessagePack serializer.
/// NOTE: This is not thread safe and should be used as a pooled resource for performance.
type TxWriter(gen : int64, ?folderPath : string, ?filePath : string) = 
    let path = defaultArg folderPath (Path.GetTempFileName())
    let mutable currentGen = gen
    let mutable fileStream = Unchecked.defaultof<_>
    
    let populateFS() = 
        let localPath = 
            if filePath.IsSome then filePath.Value
            else path +/ currentGen.ToString()
        fileStream <- new FileStream(localPath, FileMode.OpenOrCreate, 
                                     FileSystemRights.AppendData ||| FileSystemRights.WriteData, 
                                     FileShare.ReadWrite ||| FileShare.Delete, 1024, FileOptions.Asynchronous)
    
    do 
        if folderPath.IsNone && filePath.IsNone then 
            failwithf "At least one of folderPath or filePath must be provided."
        populateFS()
    
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
    static member ReadLog(localPath) = 
        if File.Exists(localPath) then 
            try 
                // NOTE: This is done to ensure that the FileStream can be opened successfully. Seq block
                // does not support try catch so there is no way to capture any errors coming out of seq block.
                use __ = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                seq {
                    use fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) 
                    while fs.Position <> fs.Length do
                        yield TransactionEntry.DeSerializer(fs)
                }
            with e -> 
                Logger.Log <| TransactionLogReadFailure(localPath, exceptionPrinter e)
                Seq.empty
        else Seq.empty
    
    interface IDisposable with
        member __.Dispose() : unit = 
            if not (isNull fileStream) then fileStream.Close()
