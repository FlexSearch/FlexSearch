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
    | Float of float32

/// Information needed to represent a field in FlexSearch document
/// This should only contain information which is fixed for a given type so that the
/// instance could be cachced. Any Index specific information shouls go to FieldSchema
[<AbstractClass>]
type FieldBase() = 
    abstract Suffix : string option
    abstract SubTypes : FieldBase [] option
    abstract FieldType : FlexSearch.Api.Constants.FieldType
    abstract LuceneFieldType : LuceneFieldType
    abstract SortFieldType : SortFieldType
    abstract DefaultStringValue : string
    abstract DefaultBasicValue : BasicDataType
    abstract ToInternal : fieldName:string -> fieldValue:string -> BasicDataType
    abstract ToExternal : fieldName:string -> fieldValue:string -> string
    abstract CreateFieldTemplate : schemaName:string -> generateDocValue:bool -> FieldTemplate
    abstract UpdateFieldTemplate : fieldName:string -> document:Dictionary<string, string> -> FieldTemplate -> unit

[<AbstractClass>]
type BasicFieldBase<'T>(fieldType, luceneFieldType, sortFieldType, defaultValueString) = 
    inherit FieldBase()
    abstract DefaultValue : 'T
    override __.Suffix = None
    override __.SubTypes = None
    override __.FieldType = fieldType
    override __.LuceneFieldType = luceneFieldType
    override __.DefaultStringValue = defaultValueString
    override __.SortFieldType = SortFieldType.SCORE
    override __.ToExternal (fieldName : string) (value : string) = value
    abstract Validate : fieldName:string -> fieldValue:string -> 'T

/// Represents a field in an Index.
type FieldSchema = 
    { SchemaName : string
      FieldName : string
      // Signifies the position of the field in the index
      Ordinal : int
      Field : FieldBase
      Analyzers : FieldAnalyzers option
      Indentity : FieldTypeIndentity
      Source : (Func<string, string, IReadOnlyDictionary<string, string>, string [], string> * string []) option }

/// KeyedCollection wrapper for Field collections
type FieldCollection() = 
    inherit KeyedCollection<string, FieldSchema>(StringComparer.OrdinalIgnoreCase)
    override __.GetKeyForItem(t : FieldSchema) = t.FieldName
    member this.TryGetValue(key : string) = this.Dictionary.TryGetValue(key)
    member this.ReadOnlyDictionary = new ReadOnlyDictionary<string, FieldSchema>(this.Dictionary)

/// Helpers for creating Lucene field types
[<RequireQualifiedAccess>]
module CreateField = 
    /// A field that is indexed but not tokenized: the entire String value is indexed as a single token. 
    /// For example this might be used for a 'country' field or an 'id' field, or any field that you 
    /// intend to use for sorting or access through the field cache.
    let string fieldName = new StringField(fieldName, Constants.StringDefaultValue, FieldStore.YES) :> LuceneField
    
    /// A field that is indexed and tokenized, without term vectors. For example this would be used on a 
    /// 'body' field, that contains the bulk of a document's text.
    let text fieldName = new TextField(fieldName, Constants.StringDefaultValue, FieldStore.YES) :> LuceneField
    
    let long fieldName = new LongField(fieldName, 0L, FieldStore.YES) :> LuceneField
    let longDV fieldName = new NumericDocValuesField(fieldName, 0L) :> LuceneField
    let int fieldName = new IntField(fieldName, 0, FieldStore.YES) :> LuceneField
    let intDV fieldName = new NumericDocValuesField(fieldName, 0L) :> LuceneField
    let double fieldName = new DoubleField(fieldName, 0.0, FieldStore.YES) :> LuceneField
    let doubleDV fieldName = new DoubleDocValuesField(fieldName, 0.0) :> LuceneField
    let float fieldName = new FloatField(fieldName, float32 0.0, FieldStore.YES) :> LuceneField
    let floatDV fieldName = new FloatDocValuesField(fieldName, float32 0.0) :> LuceneField
    let stored fieldName = new StoredField(fieldName, Constants.StringDefaultValue) :> LuceneField
    let binary fieldName = new StoredField(fieldName, [||]) :> LuceneField
    let custom (fieldName, value : string, template : FlexLucene.Document.FieldType) = 
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
    
    /// Signifies if a field is represented using multiple fields in the index
    let hasSubType (field : FieldBase) = field.SubTypes.IsSome

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

type IntField() as self = 
    inherit BasicFieldBase<Int32>(FieldType.Int, IntField.TYPE_STORED, SortFieldType.INT, "0")
    static member Instance = new IntField() :> FieldBase
    override __.DefaultValue = Convert.ToInt32 self.DefaultStringValue
    override __.DefaultBasicValue = Convert.ToInt32 self.DefaultStringValue |> Integer
    override __.Validate (fieldName : string) (value : string) = pInt self.DefaultValue value
    override this.ToInternal (fieldName : string) (value : string) = this.Validate fieldName value |> Integer
    
    override __.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
        { Fields = [| CreateField.int schemaName |]
          DocValues = 
              if generateDV then Some <| [| CreateField.intDV schemaName |]
              else None }
    
    override this.UpdateFieldTemplate (fieldName : string) (document : Dictionary<string, string>) 
             (template : FieldTemplate) = 
        let value = 
            match document.TryGetValue(fieldName) with
            | true, v -> this.Validate fieldName v
            | _ -> self.DefaultValue
        template.Fields.[0].SetIntValue(value)
        if template.DocValues.IsSome then 
            // Numeric doc values can only be saved as Int64 
            template.DocValues.Value.[0].SetLongValue(int64 value)

type DoubleField() as self = 
    inherit BasicFieldBase<Double>(FieldType.Double, DoubleField.TYPE_STORED, SortFieldType.DOUBLE, "0.0")
    static member Instance = new DoubleField() :> FieldBase
    override __.DefaultValue = Convert.ToDouble self.DefaultStringValue
    override __.DefaultBasicValue = Convert.ToDouble self.DefaultStringValue |> Double
    override __.Validate (fieldName : string) (value : string) = pDouble self.DefaultValue value
    override this.ToInternal (fieldName : string) (value : string) = this.Validate fieldName value |> Double
    
    override __.CreateFieldTemplate (schemaName : string) (generateDocValues : bool) = 
        { Fields = [| CreateField.double schemaName |]
          DocValues = 
              if generateDocValues then Some <| [| CreateField.doubleDV schemaName |]
              else None }
    
    override this.UpdateFieldTemplate (fieldName : string) (document : Dictionary<string, string>) 
             (template : FieldTemplate) = 
        let value = 
            match document.TryGetValue(fieldName) with
            | true, v -> this.Validate fieldName v
            | _ -> self.DefaultValue
        template.Fields.[0].SetDoubleValue(value)
        if template.DocValues.IsSome then template.DocValues.Value.[0].SetDoubleValue(value)

type FloatField() as self = 
    inherit BasicFieldBase<float32>(FieldType.Float, FloatField.TYPE_STORED, SortFieldType.FLOAT, "0.0")
    static member Instance = new FloatField() :> FieldBase
    override __.DefaultValue = Convert.ToSingle self.DefaultStringValue
    override __.DefaultBasicValue = Convert.ToSingle self.DefaultStringValue |> Float
    override __.Validate (fieldName : string) (value : string) = pFloat self.DefaultValue value
    override this.ToInternal (fieldName : string) (value : string) = this.Validate fieldName value |> Float
    
    override __.CreateFieldTemplate (schemaName : string) (generateDocValues : bool) = 
        { Fields = [| CreateField.float schemaName |]
          DocValues = 
              if generateDocValues then Some <| [| CreateField.floatDV schemaName |]
              else None }
    
    override this.UpdateFieldTemplate (fieldName : string) (document : Dictionary<string, string>) 
             (template : FieldTemplate) = 
        let value = 
            match document.TryGetValue(fieldName) with
            | true, v -> this.Validate fieldName v
            | _ -> self.DefaultValue
        template.Fields.[0].SetFloatValue(value)
        if template.DocValues.IsSome then template.DocValues.Value.[0].SetFloatValue(value)

type LongField(stringDefaultValue) as self = 
    inherit BasicFieldBase<Int64>(FieldType.Long, LongField.TYPE_STORED, SortFieldType.LONG, stringDefaultValue)
    static member Instance = new LongField("0") :> FieldBase
    override __.DefaultValue = Convert.ToInt64 self.DefaultStringValue
    override __.DefaultBasicValue = Convert.ToInt64 self.DefaultStringValue |> Long
    override __.Validate (fieldName : string) (value : string) = pLong self.DefaultValue value
    override this.ToInternal (fieldName : string) (value : string) = this.Validate fieldName value |> Long
    
    override __.CreateFieldTemplate (schemaName : string) (generateDocValues : bool) = 
        { Fields = [| CreateField.long schemaName |]
          DocValues = 
              if generateDocValues then Some <| [| CreateField.longDV schemaName |]
              else None }
    
    override this.UpdateFieldTemplate (fieldName : string) (document : Dictionary<string, string>) 
             (template : FieldTemplate) = 
        let value = 
            match document.TryGetValue(fieldName) with
            | true, v -> this.Validate fieldName v
            | _ -> self.DefaultValue
        template.Fields.[0].SetLongValue(value)
        if template.DocValues.IsSome then template.DocValues.Value.[0].SetLongValue(value)

type DateTimeField() as self = 
    inherit LongField("00010101000000") // Equivalent to 00:00:00.0000000, January 1, 0001, in the Gregorian calendar
    static member Instance = new DateTimeField() :> FieldBase
    override __.Validate (fieldName : string) (value : string) = 
        // TODO: Implement custom validation for datetime
        pLong self.DefaultValue value

type DateField() as self = 
    inherit LongField("00010101") // Equivalent to January 1, 0001, in the Gregorian calendar
    static member Instance = new DateTimeField() :> FieldBase
    override __.Validate (fieldName : string) (value : string) = 
        // TODO: Implement custom validation for date
        pLong self.DefaultValue value