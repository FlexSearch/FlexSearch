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

open FlexSearch.Api.Constants
open FlexLucene.Document
open FlexLucene.Search
open System
open System.Collections.Generic

module Field = 
    let powOf2 n = Math.Pow(2.0, float n) |> int
    let mutable private count = 0
    let private lookupDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    let lookup (value : string) = lookupDict.TryGetValue(value)
    
    let private add (name : string) = 
        let value = powOf2 count
        lookupDict.Add(name, value)
        count <- count + 1
        value
    
    let private alias (name : string) (value : int) = 
        lookupDict.Add(name, value)
        value
    
    /// Bit values for boolean field properties.
    /// Bit fields are also more efficient to represent in memory
    let Indexed = add "Indexed"
    
    let Tokenized = add "Tokenized"
    let Stored = add "Stored"
    let OmitNorms = add "OmitNorms"
    let OmitTfPositions = add "OmitTfPositions"
    let StoreTermVectors = add "StoreTermVectors"
    let StoreTermPositions = add "StoreTermPositions"
    let StoreTermOffsets = add "StoreTermOffsets"
    let MultiValued = add "MultiValued"
    let Required = add "Required"
    let OmitPositions = add "OmitPositions"
    let StoreOffsets = add "StoreOffsets"
    let DocValues = add "DocValues"
    let Sorted = alias "Sorted" DocValues
    let StoreTermPayloads = add "StoreTermPayloads"
    let Binary = add "Binary"
    let Int = add "Int"
    let Long = add "Long"
    let Short = add "Short"
    let String = add "String"
    let Float = add "Float"
    let Double = add "Double"
    let Numeric = Int + Long + Short + Float + Double
    
    /// Generate the identity value from the given array
    let generateIdentity (values : int []) = values |> Array.fold (|||) 0
    
    let fieldTypeToProperties (fieldType : LuceneFieldType) =
        ()//fieldType.SetNumericType(FieldTypeNumericType.)
        
    /// A field that is indexed but not tokenized: the entire String value is indexed as a single token. 
    /// For example this might be used for a 'country' field or an 'id' field, or any field that you 
    /// intend to use for sorting or access through the field cache.
    let getStringField (fieldName, value : string, store : FieldStore) = 
        new StringField(fieldName, value, store) :> LuceneField
    
    /// A field that is indexed and tokenized, without term vectors. For example this would be used on a 
    /// 'body' field, that contains the bulk of a document's text.
    let getTextField (fieldName, value, store) = new TextField(fieldName, value, store) :> LuceneField
    
    let getLongField (fieldName, value : int64, store : FieldStore) = 
        new LongField(fieldName, value, store) :> LuceneField
    let getIntField (fieldName, value : int32, store : FieldStore) = 
        new IntField(fieldName, value, store) :> LuceneField
    let getDoubleField (fieldName, value : float, store : FieldStore) = 
        new DoubleField(fieldName, value, store) :> LuceneField
    let getStoredField (fieldName, value : string) = new StoredField(fieldName, value) :> LuceneField
    let getBinaryField (fieldName) = new StoredField(fieldName, [||]) :> LuceneField
    let getField (fieldName, value : string, template : FlexLucene.Document.FieldType) = 
        new LuceneField(fieldName, value, template)
    let bytesForNullString = System.Text.Encoding.Unicode.GetBytes(Constants.StringDefaultValue)

type FieldType = 
    { Properties : int
      SearchAnalyzer : LuceneAnalyzer option
      IndexAnalyzer : LuceneAnalyzer option
      SortFieldType : SortFieldType option
      Validate : option<string -> bool>
      DefaultStringValue : string
      CreateField : int -> LuceneField
      SetValue : string -> LuceneField -> unit }

type FlexField(fieldName : string, schemaName : string, fieldType : FieldType) = 
    member __.IsIndexed() = fieldType.Properties &&& Field.Indexed <> 0
    member __.IsTokenized() = fieldType.Properties &&& Field.Tokenized <> 0
    member __.IsStored() = fieldType.Properties &&& Field.Stored <> 0
    
    member this.Store() = 
        if this.IsStored() then FieldStore.YES
        else FieldStore.NO
    
    member __.SearchAnalyzer() = fieldType.SearchAnalyzer
    member __.RequiresSearchAnalyzer() = fieldType.SearchAnalyzer.IsSome
    member __.IndexAnalyzer() = fieldType.IndexAnalyzer
    member __.RequiresIndexAnalyzer() = fieldType.IndexAnalyzer.IsSome
    member this.IsSearchable() = this.IsIndexed
    member __.HasDocValues() = fieldType.Properties &&& Field.DocValues <> 0
    member this.AllowSorting() = this.HasDocValues
    member __.IsNumericField() = fieldType.Properties &&& Field.Numeric <> 0
    member __.SortField() = fieldType.SortFieldType
    member __.CreateLuceneField() = fieldType.CreateField fieldType.Properties
    member __.SetValue(value : string, field : LuceneField) = fieldType.SetValue value field
    member __.DefaultValue() = fieldType.DefaultStringValue

module FieldExtensions = 
    let private intFieldType = ()
    let createLuceneFieldType() = ()
    let createField() = ()

module IntField =
    let private fieldProperties =
        [
            
        ]