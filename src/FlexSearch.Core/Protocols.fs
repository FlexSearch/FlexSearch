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
          mutable Position : int32
          mutable Size : int32 }
        interface IDisposable with
            member this.Dispose() = 
                if notNull this.Buffer then releaseBuffer this.Buffer
    
    let create (bufferSize : BufferSize) = 
        if not BitConverter.IsLittleEndian then 
            failwithf "FlexSearch only works with systems which support Little Endian encoding."
        { Buffer = requestBuffer bufferSize
          Position = 0
          Size = 0 }
    
    ///  Set the size of the encoded message. The size
    /// is basically the highest position encountered so far
    let inline setSize (t : T) = 
        if t.Position > t.Size then t.Size <- t.Position
    
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
        setSize t
    
    let ZeroIntEncoded = BitConverter.GetBytes(0)
    
    let utf8 = System.Text.Encoding.UTF8
    let nullByteArray = [| Byte.MinValue |]
    
    /// Encode Int64 value at the specified location into the buffer
    let encodeInt64At (value : int64) (pos : int32) (buffer : byte []) = 
        assert (buffer.Length >= pos + 8)
        buffer.[pos] <- value |> byte
        buffer.[pos + 1] <- value >>> 8 |> byte
        buffer.[pos + 2] <- value >>> 16 |> byte
        buffer.[pos + 3] <- value >>> 24 |> byte
        buffer.[pos + 4] <- value >>> 32 |> byte
        buffer.[pos + 5] <- value >>> 40 |> byte
        buffer.[pos + 6] <- value >>> 48 |> byte
        buffer.[pos + 7] <- value >>> 56 |> byte
    
    /// Encode Int64 value into the buffer
    let encodeInt64 (value : int64) (t : T) = 
        encodeInt64At value t.Position t.Buffer
        t.Position <- t.Position + 8
        setSize t
    
    /// Decode Int64 value at specified position from the buffer
    let decodeInt64At (pos : int32) (buffer : byte []) = 
        assert (buffer.Length >= pos + 8)
        let res = 
            int64 buffer.[pos] ||| (int64 buffer.[pos + 1] <<< 8) ||| (int64 buffer.[pos + 2] <<< 16) 
            ||| (int64 buffer.[pos + 3] <<< 24) ||| (int64 buffer.[pos + 4] <<< 32) ||| (int64 buffer.[pos + 5] <<< 40) 
            ||| (int64 buffer.[pos + 6] <<< 48) ||| (int64 buffer.[pos + 7] <<< 56)
        res
    
    /// Decode Int64 value from the buffer
    let decodeInt64 (t : T) = 
        let res = decodeInt64At t.Position t.Buffer
        t.Position <- t.Position + 8
        res
    
    /// Encode Int32 value at the given position
    let encodeInt32At (value : int32) (pos : int32) (buffer : byte []) = 
        assert (buffer.Length >= pos + 4)
        buffer.[pos] <- value |> byte
        buffer.[pos + 1] <- value >>> 8 |> byte
        buffer.[pos + 2] <- value >>> 16 |> byte
        buffer.[pos + 3] <- value >>> 24 |> byte
    
    /// Encode Int32 value into the buffer
    let encodeInt32 (value : int32) (t : T) = 
        encodeInt32At value t.Position t.Buffer
        t.Position <- t.Position + 4
        setSize t
    
    /// Decode Int32 value from the buffer
    let decodeInt32At (pos : int32) (buffer : byte []) = 
        assert (buffer.Length >= pos + 4)
        let res = 
            int32 buffer.[pos] ||| (int32 buffer.[pos + 1] <<< 8) ||| (int32 buffer.[pos + 2] <<< 16) 
            ||| (int32 buffer.[pos + 3] <<< 24)
        res
    
    /// Decode Int32 value from the buffer
    let decodeInt32 (t : T) = 
        let res = decodeInt32At t.Position t.Buffer
        t.Position <- t.Position + 4
        res
    
    /// Move the position to the start of next word boundary
    let align (t : T) = 
        // Add padding to ensure correct 8 byte alignment
        let mutable alignment = t.Position % 8
        if alignment <> 0 then
            for i = 0 to 8 - alignment - 1 do
                append nullByteArray t
        setSize t
    
    /// Encode a string value into the buffer
    let encodeString (value : string) (t : T) = 
        assert (notNull value)
        let src = utf8.GetBytes(value)
        append src t
        src.Length
    
    /// Decode a string value from the buffer
    let decodeStringAt (pos : int) (length : int) (buffer : byte []) = 
        assert (buffer.Length >= pos + length)
        let res = utf8.GetString(buffer, pos, length)
        res

(*
                                                                                  |<- 8 byte aligned
+----------+------+-------+-------+------------+----------+-- ................ ---+---------+- ..... -+
| Version  | Size | TxID  | Field | F(0) Start | F(0)     | F(n) Start | F(n)     | F(0)    | F(n)    |
|          |      |       | Count | Position   | Length   | Position   | Length   | Content | Content |
+----------+------+-------+-------+------------+----------+-- ................ ---+---------+- ..... -+
|<-- 4B -->|< 4B >|<  8B >|<- 4B->|<--- 4B --->|<-- 4B -->|                       |<- Var ->|

  TxID = Transaction ID
  F Count = Total no of Field
  F(n) Location = Nth Field location
  F(n) Content = Nth Field Content
  Var = Variable length
*)
module DocumentProtocol = 
    let versionFieldPos = 0
    let versionFieldLen = 4
    let sizeFieldPos = versionFieldPos + versionFieldLen
    let sizeFieldLen = 4
    let txIdFieldPos = sizeFieldPos + sizeFieldLen
    let txIdFieldLen = 8
    let countFieldPos = txIdFieldPos + txIdFieldLen
    let countFieldLen = 4
    let infoFieldPosition = countFieldPos + countFieldLen
    let infoFieldLen = 8
    
    let encodeVersion (t : T) = 
        t.Position <- versionFieldPos
        encodeInt32 1 t
    
    let decodeVersion (t : T) = 
        t.Position <- versionFieldPos
        decodeInt32 t
    
    /// Encode the transaction Id
    let encodeTransactionId (txId : int64) (t : T) = 
        t.Position <- txIdFieldPos
        encodeInt64 txId t
    
    /// Returns the Transaction ID encoded in the message
    let decodeTransactionId (t : T) = 
        t.Position <- txIdFieldPos
        decodeInt64 t
    
    let encodeFieldCount (count : int32) (t : T) = 
        t.Position <- countFieldPos
        encodeInt32 count t
    
    let decodeFieldCount (t : T) = 
        t.Position <- countFieldPos
        decodeInt32 t
    
    /// Encode a field information like the message start and end position
    let encodeFieldInfo (pos : int32) (t : T) (startPosition : int, length : int) = 
        encodeInt32At startPosition (infoFieldPosition + pos * infoFieldLen) t.Buffer
        encodeInt32At length (infoFieldPosition + pos * infoFieldLen + 4) t.Buffer

    /// Decode a field information like the message start and end position
    let decodeFieldInfo (pos : int32) (t : T) = 
        let startPos = decodeInt32At (infoFieldPosition + pos * infoFieldLen) t.Buffer
        let length = decodeInt32At (infoFieldPosition + pos * infoFieldLen + 4) t.Buffer
        (startPos, length)
    
    /// Encode field data
    let encodeFieldData (data : string) (t : T) = 
        align t
        let startPos = t.Position
        let len = encodeString data t
        (startPos, len)
    
    // Decode field data
    let decodeFieldData (pos : int) (t : T) = 
        let startPos, len = decodeFieldInfo pos t
        if startPos <> 0 then 
            t.Position <- startPos
            decodeStringAt startPos len t.Buffer
        else String.Empty
    
    let encodeSize (t : T) = 
        t.Position <- sizeFieldPos
        encodeInt32 t.Size t
    
    let decodeSize (t : T) = 
        t.Position <- sizeFieldPos
        t.Size <- decodeInt32 t
        t.Size
    
    let intialize (txId : int64) (fieldCount : int32) (t : T) = 
        encodeVersion t
        encodeTransactionId txId t
        encodeFieldCount fieldCount t
        // Add a default start position and length for each field
        for i = 0 to fieldCount - 1 do
            encodeFieldInfo i t (0, 0)
            t.Position <- t.Position + 8
        setSize t
    
    /// Encode a document to a byte array
    let encodeDocument (txId : int64, document : Dictionary<string, string>) = 
        let message = DocumentBuffer.create (BufferSize.Small)
        intialize txId document.Count message
        let mutable count = 0
        for pair in document do
            // The start position and length of 0 signify that the value is null
            // which is the default encoded value
            if isNotBlank pair.Value then
                let pos, len = encodeFieldData pair.Value message
                encodeFieldInfo count message (pos, len)
            count <- count + 1
        encodeSize message
        message
