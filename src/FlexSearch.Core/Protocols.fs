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

open System
open System.IO
open System.Collections.Generic
open System.Linq
open System.Text

[<AutoOpen>]
module DocumentBuffer = 
    type T = 
        { mutable Buffer : byte array
          mutable Position : int32 }
        interface IDisposable with
            member this.Dispose() = 
                if notNull this.Buffer then releaseBuffer this.Buffer
    
    let create (bufferSize : BufferSize) = 
        if not BitConverter.IsLittleEndian then 
            failwithf "FlexSearch only works with systems which support Little Endian encoding."
        { Buffer = requestBuffer bufferSize
          Position = 0 }
    
    let append (src : byte []) (t : T) = 
        let bounds = t.Position + src.Length
        /// It doesn't make sense to upgrade to large buffer from the pool
        /// if it is still smaller then the required size
        if bounds >= t.Buffer.Length && bounds <= BytePool.largePoolSizeBytes then 
            let newBuffer = requestBuffer BufferSize.Large
            Buffer.BlockCopy(t.Buffer, 0, newBuffer, 0, t.Buffer.Length)
            releaseBuffer t.Buffer
            t.Buffer <- newBuffer
        else 
            if bounds > BytePool.largePoolSizeBytes then 
                // Allocate a new byte array outside the buffer pool
                // Make it twice the size of the current bound to cater
                // for future
                let newBuffer = Array.create (bounds * 2 * 1024) (0uy)
                Buffer.BlockCopy(t.Buffer, 0, newBuffer, 0, t.Buffer.Length)
                releaseBuffer t.Buffer
                t.Buffer <- newBuffer
        /// PERF: Don't use BlockCopy for a copying single element array
        if src.Length = 1 then t.Buffer.[t.Position] <- src.[0]
        else Buffer.BlockCopy(src, 0, t.Buffer, t.Position, src.Length)
        t.Position <- bounds
    
    let ZeroIntEncoded = BitConverter.GetBytes(0)
    
    let utf8 = System.Text.Encoding.UTF8
    let nullByteArray = [| Byte.MinValue |]
    
    /// Encode Int64 value into the buffer
    let encodeInt64 (value : int64) (t : T) = 
        assert (t.Buffer.Length >= t.Position + 8)
        t.Buffer.[t.Position] <- value |> byte
        t.Buffer.[t.Position + 1] <- value >>> 8 |> byte
        t.Buffer.[t.Position + 2] <- value >>> 16 |> byte
        t.Buffer.[t.Position + 3] <- value >>> 24 |> byte
        t.Buffer.[t.Position + 4] <- value >>> 32 |> byte
        t.Buffer.[t.Position + 5] <- value >>> 40 |> byte
        t.Buffer.[t.Position + 6] <- value >>> 48 |> byte
        t.Buffer.[t.Position + 7] <- value >>> 56 |> byte
        t.Position <- t.Position + 8
    
    /// Decode Int64 value from the buffer
    let decodeInt64 (t : T) = 
        assert (t.Buffer.Length >= t.Position + 8)
        let res = 
            int64 t.Buffer.[t.Position] ||| (int64 t.Buffer.[t.Position + 1] <<< 8) 
            ||| (int64 t.Buffer.[t.Position + 2] <<< 16) ||| (int64 t.Buffer.[t.Position + 3] <<< 24) 
            ||| (int64 t.Buffer.[t.Position + 4] <<< 32) ||| (int64 t.Buffer.[t.Position + 5] <<< 40) 
            ||| (int64 t.Buffer.[t.Position + 6] <<< 48) ||| (int64 t.Buffer.[t.Position + 7] <<< 56)
        t.Position <- t.Position + 8
        res
    
    /// Encode Int32 value into the buffer
    let encodeInt32 (value : int32) (t : T) = 
        assert (t.Buffer.Length >= t.Position + 4)
        t.Buffer.[t.Position] <- value |> byte
        t.Buffer.[t.Position + 1] <- value >>> 8 |> byte
        t.Buffer.[t.Position + 2] <- value >>> 16 |> byte
        t.Buffer.[t.Position + 3] <- value >>> 24 |> byte
        t.Position <- t.Position + 4
    
    /// Decode Int32 value from the buffer
    let decodeInt32 (t : T) = 
        assert (t.Buffer.Length >= t.Position + 4)
        let res = 
            int32 t.Buffer.[t.Position] ||| (int32 t.Buffer.[t.Position + 1] <<< 8) 
            ||| (int32 t.Buffer.[t.Position + 2] <<< 16) ||| (int32 t.Buffer.[t.Position + 3] <<< 24)
        t.Position <- t.Position + 4
        res
    
    /// Move the position to the start of next word boundary
    let align (t : T) = 
        // Add padding to ensure correct 8 byte alignment
        let mutable alignment = t.Position % 8
        while alignment <> 0 do
            append nullByteArray t
            alignment <- alignment - 1
    
    /// Encode a string value into the buffer
    let encodeString (value : string) (t : T) = 
        assert (notNull value)
        let src = utf8.GetBytes(value)
        append src t
        src.Length

    /// Decode a string value from the buffer
    let decodeString (length : int) (t : T) = 
        assert (t.Buffer.Length >= t.Position + length)
        let res = utf8.GetString(t.Buffer, t.Position, length)
        t.Position <- t.Position + length
        res

(*
                                                                       |<- 8 byte aligned
+------+-------+-------+------------+----------+-- ................ ---+---------+- ..... -+
| Size | TxID  | Field | F(0) Start | F(0) End | F(n) Start | F(n) End | F(0)    | F(n)    |
|      |       | Count | Position   | Position | Position   | Position | Content | Content |
+------+-------+-------+------------+----------+-- ................ ---+---------+- ..... -+
<- 4B->|<- 8B ->|<- 4B->|<--- 4B --->|<-- 4B -->|                       |<- Var ->|

  TxID = Transaction ID
  F Count = Total no of Field
  F(n) Location = Nth Field location
  F(n) Content = Nth Field Content
  Var = Variable length
*)
module DocumentProtocol = 
    /// Encode the transaction Id
    let encodeTransactionId (txId : int64) (t : T) = 
        // Transaction ID should always be at the beginning of the message
        t.Position <- 0
        encodeInt64 txId t
    
    /// Returns the Transaction ID encoded in the message
    let decodeTransactionId (t : T) = 
        t.Position <- 0
        decodeInt64 t
    
    let intialize (txId : int64) (fieldCount : int32) (t : T) = 
        // Add the transaction log id
        encodeInt64 txId t
        encodeInt32 fieldCount t
        // Add a default location for each field
        for i = 0 to fieldCount do
            append ZeroIntEncoded t
    
    /// Update the starting position of an item in the document
    let updatePos (fieldPos : int) (location : int) (t : T) = 
        let src = BitConverter.GetBytes(location)
        // Copy the 4 bytes directly rather than using buffer copy
        src |> Array.iteri (fun i value -> t.Buffer.[8 + 4 * fieldPos + i] <- value)
    
    let encodeDocument (txId : int64, document : Dictionary<string, string>) = 
        let message = DocumentBuffer.create (BufferSize.Small)
        intialize txId document.Count message
        let mutable count = 1
        for pair in document do
            // Align the position before writing to the array
            align message
            // write the current position as the starting position for the value in memory
            updatePos count message.Position message
            encodeString pair.Value message
        message
