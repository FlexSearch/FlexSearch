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

open Microsoft.Extensions.ObjectPool
open System
open System.Collections.Concurrent
open System.Threading

type DefaultObjectPoolPolicy<'T>(factory : unit -> 'T, onRelease : 'T -> bool) = 
    interface IPooledObjectPolicy<'T> with
        member __.Create() = factory()
        member __.Return(value : 'T) = onRelease (value)

[<AutoOpen>]
module Pools = 
    open System.Collections.Generic
    open System
    
    /// A reusable dictionary pool
    let dictionaryPool = 
        let initialCapacity = 50
        let itemsInPool = 300
        let factory() = new Dictionary<string, string>(initialCapacity, StringComparer.OrdinalIgnoreCase)
        
        let onRelease (dict : Dictionary<string, string>) = 
            dict.Clear()
            true
        new DefaultObjectPool<Dictionary<string, string>>(new DefaultObjectPoolPolicy<Dictionary<string, string>>(factory, 
                                                                                                              onRelease))
    
    /// Global memory pool to be shared across the engine
    let memory = new Microsoft.IO.RecyclableMemoryStreamManager()

[<AutoOpen>]
module BytePool = 
    type BufferSize = 
        | Small = 0
        | Large = 1
    
    let utf = Text.UTF8Encoding.UTF8
    
    let private generatePool (sizeInKB : int, itemsInPool : int) = 
        let factory() = Array.create (sizeInKB * 1024) (0uy)
        let onRelease (item : _) = true
        new DefaultObjectPool<byte []>(new DefaultObjectPoolPolicy<byte []>(factory, onRelease))
    
    let smallPoolSize = 128
    let largePoolSize = 4096
    let smallPoolSizeBytes = smallPoolSize * 1024
    let largePoolSizeBytes = largePoolSize * 1024
    let private smallPool = generatePool (smallPoolSize, Environment.ProcessorCount * 2)
    let private largePool = generatePool (largePoolSize, Environment.ProcessorCount * 2)
    
    let requestBuffer (bufferSize : BufferSize) = 
        match bufferSize with
        | BufferSize.Small -> smallPool.Get()
        | BufferSize.Large -> largePool.Get()
        | _ -> failwithf "Internal : Unsupported buffer size requested."
    
    let releaseBuffer (item : array<byte>) = 
        if item.Length = smallPoolSizeBytes then smallPool.Return(item)
        else 
            if item.Length = largePoolSizeBytes then largePool.Return(item)

open Microsoft.Extensions.ObjectPool
type TrackingObjectPool<'T when 'T : equality and 'T : not struct> (policy : IPooledObjectPolicy<'T>, maximumRetained : int) =
    let _items : 'T[] = Array.init maximumRetained (fun i -> Unchecked.defaultof<'T>)
    let _policy : IPooledObjectPolicy<'T> = if policy |> isNull 
                                            then raise <| new ArgumentNullException("policy")
                                            else policy
    
    let rec iterator index (array : 'X[] when 'X : equality and 'X : not struct)  = 
        if index >= array.Length then None
        else 
            let item = array.[index]
            if item <> Unchecked.defaultof<'X> 
               && Interlocked.CompareExchange(ref array.[index], Unchecked.defaultof<'X>, item) = item
            then Some(item)
            else iterator (index + 1) array

    let rec replaceFirstNullWith index obj (array : 'X[]) =
        if index >= array.Length then ()
        else
            if array.[index] = Unchecked.defaultof<'X> then array.[index] <- obj
            else replaceFirstNullWith (index + 1) obj array

    new (policy : IPooledObjectPolicy<'T>) = TrackingObjectPool(policy, Environment.ProcessorCount * 2)

    member __.Get() = 
        match _items |> iterator 0 with
        | Some(item) -> item
        | _ -> _policy.Create()
    member __.Return(t : 'T) =
        if not <| _policy.Return(t) then ()
        else _items |> replaceFirstNullWith 0 t
    member __.GetAll() = _items |> Array.where ((<>) Unchecked.defaultof<'T>)