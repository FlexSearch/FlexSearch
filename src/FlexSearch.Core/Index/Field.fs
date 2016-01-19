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

/// Uniquely represents the properties of a field Type
type FieldTypeIndentity = 
    { Value : int32 }

/// Represents the analyzers associated with a field. By creating this abstraction
/// we can easily create a cacheable copy of it which can be shared across field types
type FieldAnalyzers = 
    { SearchAnalyzer : LuceneAnalyzer
      IndexAnalyzer : LuceneAnalyzer }

/// Represents the minimum unit to represent a field in FlexSearch Document. The reson
/// to use array is to support fields which can maps to multiple internal fields.
/// Note: We will create a new instance of FieldTemplate per field in an index. So, it
/// should not occupy a lot of memory
type FieldTemplate = 
    { Fields : LuceneField []
      DocValues : LuceneField [] option }

type BasicDataType = 
    | String of string
    | Integer of int32
    | Long of int64
    | Double of double

/// Information needed to represent a field in FlexSearch document
/// This should only contain information which is fixed for a given type so that the
/// instance could be cachced. Any Index specific information shouls go to FieldSchema
[<Interface>]
type IField = 
    
    /// Signifies if a field is represented using multiple fields in the index
    abstract IsMultiField : bool
    
    abstract Suffix : string option
    abstract SubTypes : IField [] option
    abstract FieldType : FlexSearch.Api.Constants.FieldType
    abstract LuceneFieldType : LuceneFieldType
    abstract SortFieldType : SortFieldType option
    abstract DefaultStringValue : string
    abstract ToInternal : string -> BasicDataType
    abstract ToExternal : fieldName:string -> fieldValue:string -> string
    abstract Validate : fieldName:string -> fieldValue:string -> BasicDataType option
    abstract DefaultValue : BasicDataType
    abstract CreateFieldTemplate : schemaName:string -> generateDocValue:bool -> FieldTemplate
    abstract UpdateFieldTemplate : fieldName:string -> document:Dictionary<string, string> -> FieldTemplate -> unit

/// Represents a field in an Index.
type FieldSchema = 
    { SchemaName : string
      FieldName : string
      // Signifies the position of the field in the index
      Ordinal : int
      Field : IField
      Analyzers : FieldAnalyzers option
      Indentity : FieldTypeIndentity
      Source : (Func<string, string, IReadOnlyDictionary<string, string>, string [], string> * string []) option }

/// KeyedCollection wrapper for Field collections
type FieldCollection() = 
    inherit KeyedCollection<string, FieldSchema>(StringComparer.OrdinalIgnoreCase)
    override __.GetKeyForItem(t : FieldSchema) = t.FieldName
    member this.TryGetValue(key : string) = this.Dictionary.TryGetValue(key)
    member this.ReadOnlyDictionary = new ReadOnlyDictionary<string, FieldSchema>(this.Dictionary)

module LuceneFieldHelpers = 
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
    
    /// Returns the Index analyzer assicuated with the field 
    let indexAnalyzer (schema : FieldSchema) = 
        match schema.Analyzers with
        | Some(a) -> Some(a.IndexAnalyzer)
        | _ -> None
    
    /// Signifies if the field is searchable
    let isSearchable (schema : FieldSchema) = schema |> isIndexed
    
    /// Signifies if the field supports doc values
    let hasDocValues (schema : FieldSchema) = schema.Indentity.Value &&& DocValues <> 0
    
    /// Signifies if the field allows sorting
    let allowSorting (schema : FieldSchema) = schema |> hasDocValues
    
    /// Signifies if the field is numeric
    let isNumericField (schema : FieldSchema) = schema.Indentity.Value &&& Numeric <> 0
    
    /// Returns the Sort field associated with the field
    let sortField (schema : FieldSchema) = schema.Field.SortFieldType

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TypeIndentity = 
    /// Generate the identity value from the given array
    let generateIdentity (values : int []) = { Value = values |> Array.fold (|||) 0 }
    
    /// Generates the field properties identity from the Lucene Field Type
    let createFromFieldType (fieldType : LuceneFieldType) = 
        let properties = new ResizeArray<int>()
        if fieldType.Tokenized() then properties.Add(Field.Tokenized)
        if fieldType.Stored() then properties.Add(Field.Stored)
        if fieldType.OmitNorms() then properties.Add(Field.OmitNorms)
        let indexOptions = fieldType.IndexOptions()
        // Default is DOCS_AND_FREQS_AND_POSITIONS           
        if indexOptions = IndexOptions.DOCS then properties.Add(Field.OmitTfPositions)
        else if indexOptions = IndexOptions.DOCS_AND_FREQS then properties.Add(Field.OmitPositions)
        else 
            if indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS then 
                properties.Add(Field.StoreOffsets)
        if fieldType.StoreTermVectors() then properties.Add(Field.StoreTermVectors)
        if fieldType.StoreTermVectorOffsets() then properties.Add(Field.StoreTermOffsets)
        if fieldType.StoreTermVectorPositions() then properties.Add(Field.StoreTermPositions)
        if fieldType.StoreTermVectorPayloads() then properties.Add(Field.StoreTermPayloads)
        properties.ToArray() |> generateIdentity

module IntFieldExtensions = 
    let Properties = TypeIndentity.createFromFieldType (IntField.TYPE_STORED)
    let LuceneFieldType = IntField.TYPE_STORED
    let SortFieldType = Some <| SortFieldType.INT
    let Analyzers = None
    let DefaultValue = 0
    let DefaultStringvalue = "0"

type IntField() = 
    static member Default = new IntField() :> IField
    interface IField with
        member __.IsMultiField = false
        member __.FieldType = FlexSearch.Api.Constants.FieldType.Int
        member __.LuceneFieldType = IntFieldExtensions.LuceneFieldType
        member __.SortFieldType = IntFieldExtensions.SortFieldType
        member __.SubTypes = None
        member __.Suffix = None
        member __.DefaultValue = Integer IntFieldExtensions.DefaultValue
        member __.DefaultStringValue = IntFieldExtensions.DefaultStringvalue
        member __.ToInternal(value : string) = pInt IntFieldExtensions.DefaultValue value |> Integer
        member __.ToExternal (fieldName : string) (value : string) = value
        member __.Validate (fieldName : string) (value : string) = 
            pInt IntFieldExtensions.DefaultValue value
            |> Integer
            |> Some
        member __.CreateFieldTemplate(schemaName: string) (generateDocValues : bool) = 
                { Fields = [| LuceneFieldHelpers.getIntField(schemaName, IntFieldExtensions.DefaultValue, FieldStore.YES) |]
                  DocValues = None } // TODO: Generate DocValues conditionally
        member __.UpdateFieldTemplate (fieldName:string) (document:Dictionary<string, string>) (template:FieldTemplate) = ()
    