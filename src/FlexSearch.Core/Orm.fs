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

/// The purpose of this module is to provide generic helper to map 
/// internal types to FlexSearch documents so that it could be used
/// to easily add/retrieve strongly typed objects from FlexSearch 
/// indices. This module will be used for logging, duplicates, jobs 
/// etc.
module Orm =
    open FlexSearch.Api.Model
    open FastMember
    open System.Collections.Concurrent
    open System.Collections.Generic
    open System

    let private typeRepository = conDict<TypeAccessor>()
    let private dateTimeType = typeof<DateTime>.FullName

    let inline private generateFields<'T> (instance : 'T) (accessor : TypeAccessor) =
        let doc = new Dictionary<string,string>()
        for m in accessor.GetMembers() do
            if m.Type.FullName = dateTimeType then
                let date = accessor.[instance, m.Name] :?> DateTime
                doc.Add(m.Name, dateToFlexFormat(date).ToString()) 
            else
                doc.Add(m.Name, (string)accessor.[instance, m.Name])
        doc

    /// Create flex document from the object using reflection
    let createDocFromObj<'T> indexName id (instance: 'T) =
        let doc = new Document(id, indexName)
        
        match typeRepository.TryGetValue(typeof<'T>.FullName) with
        | true, t -> doc.Fields <- generateFields<'T> (instance) t
        | _ -> 
            typeRepository.[typeof<'T>.FullName] <- TypeAccessor.Create(typeof<'T>)
            doc.Fields <- generateFields<'T> instance typeRepository.[typeof<'T>.FullName]
        doc

    