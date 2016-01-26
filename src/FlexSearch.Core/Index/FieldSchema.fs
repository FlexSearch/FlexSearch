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
open FlexLucene.Index
open FlexLucene.Search
open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Text

/// Uniquely represents the properties of a field Type
type FieldTypeIndentity = 
    { Value : int32 }

/// Represents a field in an Index.
type FieldSchema = 
    { SchemaName : string
      FieldName : string
      // Signifies the position of the field in the index
      Ordinal : int
      Field : BasicFieldType
      Analyzers : FieldAnalyzers option
      Similarity : Similarity
      Indentity : FieldTypeIndentity
      Source : (Func<string, string, IReadOnlyDictionary<string, string>, string [], string> * string []) option }

/// KeyedCollection wrapper for Field collections
type FieldCollection() = 
    inherit KeyedCollection<string, FieldSchema>(StringComparer.OrdinalIgnoreCase)
    override __.GetKeyForItem(t : FieldSchema) = t.FieldName
    member this.TryGetValue(key : string) = this.Dictionary.TryGetValue(key)
    member this.ReadOnlyDictionary = new ReadOnlyDictionary<string, FieldSchema>(this.Dictionary)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FieldSchema = 
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
    
    /// Signifies if a field is indexed
    let isIndexed (schema : FieldSchema) = schema.Indentity.Value &&& Indexed <> 0
    
    /// Signifies if a field is tokenized
    let isTokenized (schema : FieldSchema) = schema.Indentity.Value &&& Tokenized <> 0
    
    /// Signifies if a field is stored
    let isStored (schema : FieldSchema) = schema.Indentity.Value &&& Stored <> 0
    
    /// Method to map boolean to FieldStore enum
    let store (schema : FieldSchema) = 
        if schema |> isStored then FieldStore.YES
        else FieldStore.NO
    
    /// Signifies if the field requires a search time analyzer
    let requiresSearchAnalyzer (schema : FieldSchema) = schema.Analyzers.IsSome
    
    /// Signifies if the field requires an index time analyzer
    let requiresIndexAnalyzer (schema : FieldSchema) = schema.Analyzers.IsSome
    
    /// Returns the Search analyzer associated with the field
    let searchAnalyzer (schema : FieldSchema) = 
        match schema.Analyzers with
        | Some(a) -> Some(a.SearchAnalyzer)
        | _ -> None
    
    /// Returns the Index analyzer associated with the field 
    let indexAnalyzer (schema : FieldSchema) = 
        match schema.Analyzers with
        | Some(a) -> Some(a.IndexAnalyzer)
        | _ -> None
    
    /// Signifies if the field is search-able
    let isSearchable (schema : FieldSchema) = schema |> isIndexed
    
    /// Signifies if the field supports doc values
    let hasDocValues (schema : FieldSchema) = schema.Indentity.Value &&& DocValues <> 0
    
    /// Signifies if the field allows sorting
    let allowSorting (schema : FieldSchema) = schema |> hasDocValues
    
    /// Signifies if the field is numeric
    let isNumericField (schema : FieldSchema) = schema.Indentity.Value &&& Numeric <> 0
    
    /// Returns all the metadata fields that should be present in an index    
    let getMetaFields() = 
        [| IdField.Instance; TimeStampField.Instance; ModifyIndexField.Instance; StateField.Instance |]
    
    let getMetaFieldsTemplates() = 
        let getTemplateFields (fieldType : BasicFieldType) = 
            match fieldType with
            | StringType(v) -> v.CreateFieldTemplate "" false
            | LongType(v) -> v.CreateFieldTemplate "" true
            | _ -> failwithf "Meta fields for other types are not supported."
        getMetaFields() |> Array.map getTemplateFields

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TypeIndentity = 
    /// Generate the identity value from the given array
    let generateIdentity (values : int []) = { Value = values |> Array.fold (|||) 0 }
    
    /// Generates the field properties identity from the Lucene Field Type
    let createFromFieldType (fieldType : LuceneFieldType) = 
        let properties = new ResizeArray<int>()
        if fieldType.Tokenized() then properties.Add(FieldSchema.Tokenized)
        if fieldType.Stored() then properties.Add(FieldSchema.Stored)
        if fieldType.OmitNorms() then properties.Add(FieldSchema.OmitNorms)
        let indexOptions = fieldType.IndexOptions()
        // Default is DOCS_AND_FREQS_AND_POSITIONS           
        if indexOptions = IndexOptions.DOCS then properties.Add(FieldSchema.OmitTfPositions)
        else if indexOptions = IndexOptions.DOCS_AND_FREQS then properties.Add(FieldSchema.OmitPositions)
        else 
            if indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS then 
                properties.Add(FieldSchema.StoreOffsets)
        if fieldType.StoreTermVectors() then properties.Add(FieldSchema.StoreTermVectors)
        if fieldType.StoreTermVectorOffsets() then properties.Add(FieldSchema.StoreTermOffsets)
        if fieldType.StoreTermVectorPositions() then properties.Add(FieldSchema.StoreTermPositions)
        if fieldType.StoreTermVectorPayloads() then properties.Add(FieldSchema.StoreTermPayloads)
        properties.ToArray() |> generateIdentity
