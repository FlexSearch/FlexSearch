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
      FieldType : FieldBase
      Analyzers : FieldAnalyzers option
      Similarity : Similarity
      TypeIdentity : FieldTypeIndentity }

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
    let StoreOnly = add "StoreOnly"
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
    let isIndexed (schema : FieldSchema) = schema.TypeIdentity.Value &&& Indexed <> 0
    
    /// Signifies if a field is tokenized
    let isTokenized (schema : FieldSchema) = schema.TypeIdentity.Value &&& Tokenized <> 0
    
    /// Signifies if a field is stored
    let isStored (schema : FieldSchema) = schema.TypeIdentity.Value &&& Stored <> 0
    
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
    let isSearchable (schema : FieldSchema) = schema.TypeIdentity.Value &&& StoreOnly = 0
    
    /// Signifies if the field supports doc values
    let hasDocValues (schema : FieldSchema) = schema.TypeIdentity.Value &&& DocValues <> 0
    
    /// Signifies if the field allows sorting
    let allowSorting (schema : FieldSchema) = schema |> hasDocValues
    
    /// Signifies if the field is numeric
    let isNumericField (schema : FieldSchema) = schema.TypeIdentity.Value &&& Numeric <> 0
    
    /// Generate the identity value from the given array
    let generateIdentity (values : int []) = { Value = values |> Array.fold (|||) 0 }
    
    type FlexField = FlexSearch.Api.Model.Field
    
    type FlexFieldType = FlexSearch.Api.Constants.FieldType
    
    /// Generates the field properties identity from the Lucene Field Type
    let createFromFieldType (fieldType : LuceneFieldType) (field : FlexField) = 
        let properties = new ResizeArray<int>()
        if field.FieldType = FlexFieldType.Stored then properties.Add(StoreOnly)
        if fieldType.Tokenized() then properties.Add(Tokenized)
        if fieldType.Stored() then properties.Add(Stored)
        if fieldType.OmitNorms() then properties.Add(OmitNorms)
        let indexOptions = fieldType.IndexOptions()
        // Default is DOCS_AND_FREQS_AND_POSITIONS           
        if indexOptions = IndexOptions.DOCS then properties.Add(OmitTfPositions)
        else if indexOptions = IndexOptions.DOCS_AND_FREQS then properties.Add(OmitPositions)
        else 
            if indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS then properties.Add(StoreOffsets)
        if fieldType.StoreTermVectors() then properties.Add(StoreTermVectors)
        if fieldType.StoreTermVectorOffsets() then properties.Add(StoreTermOffsets)
        if fieldType.StoreTermVectorPositions() then properties.Add(StoreTermPositions)
        if fieldType.StoreTermVectorPayloads() then properties.Add(StoreTermPayloads)
        if field.AllowSort then properties.Add(DocValues)
        let numericType = fieldType.NumericType()
        if numericType = FieldTypeLegacyNumericType.DOUBLE then
            properties.Add(Double)
        else if numericType = FieldTypeLegacyNumericType.FLOAT then
            properties.Add(Float)
        else if numericType = FieldTypeLegacyNumericType.INT then
            properties.Add(Int)
        else if numericType = FieldTypeLegacyNumericType.LONG then
            properties.Add(Long)
        properties.ToArray() |> generateIdentity
    
    /// Build a Schema field from the Field DTO
    let build (field : FlexField) (getAnalyzer : GetAnalyzer) = 
        let getFieldType (field : FlexField) = 
            match field.FieldType with
            | FieldType.Int -> IntField.Instance
            | FieldType.Double -> DoubleField.Instance
            | FieldType.Float -> FloatField.Instance
            | FieldType.Bool -> BoolField.Instance
            | FieldType.Date -> DateField.Instance
            | FieldType.DateTime -> DateTimeField.Instance
            | FieldType.Long -> LongField.Instance
            | FieldType.Stored -> StoredField.Instance
            | FieldType.Keyword -> ExactTextField.Instance
            | FieldType.Text -> TextField.Instance
            | _ -> failwithf "Internal error: Unsupported FieldType"
        
        let getAnalyzers (field : FlexField) = 
            maybe { 
                // These are the only two field types which support custom analyzer
                if field.FieldType = FieldType.Text then 
                    let! searchAnalyzer = getAnalyzer field.SearchAnalyzer
                    let! indexAnalyzer = getAnalyzer field.IndexAnalyzer
                    return Some <| { IndexAnalyzer = indexAnalyzer
                                     SearchAnalyzer = searchAnalyzer }
                else return None
            }
        
        maybe { 
            let basicFieldType = getFieldType field
            let! analyzers = getAnalyzers field
            let typeIdentity = createFromFieldType (basicFieldType.LuceneFieldType) field
            return { FieldName = field.FieldName
                     SchemaName = field.FieldName
                     FieldType = basicFieldType
                     TypeIdentity = typeIdentity
                     Similarity = field.Similarity
                     Analyzers = analyzers }
        }
    
    /// Get tokens for a given input. This is not supported by all field types for example
    /// it does not make any sense to tokenize numeric types and exact text fields. In these
    /// cases the internal representation of the field type is used.
    /// Note: An instance of List is passed so that we can avoid memory allocation and
    /// reuse the list from the object pool.
    /// Note: Avoid using the below for numeric types. 
    let inline getTokens (value : string, result : List<string>) (fs : FieldSchema) = 
        match fs.Analyzers with
        | Some(analyzers) ->
            parseTextUsingAnalyzer (analyzers.SearchAnalyzer, fs.SchemaName, value, result)
        | _ -> 
            // The field does not have an associated analyzer so just add the input to
            // the result by using the field specific formatting
            result.Add(fs.FieldType.ToInternal value)

    ///----------------------------------------------------------------------
    /// Meta data fields related
    ///----------------------------------------------------------------------
    /// Helper method to generate FieldSchema for a given meta data field
    let generateSchemaForMetaField name basicFieldType docValues = 
        let field = new FlexField()
        field.AllowSort <- docValues
        { FieldName = name
          SchemaName = name
          FieldType = basicFieldType
          TypeIdentity = createFromFieldType (basicFieldType.LuceneFieldType) field
          Similarity = FlexSearch.Api.Constants.Similarity.TFIDF
          Analyzers = None }
    
    /// Returns all the meta-data fields that should be present in an index    
    let getMetaFields = [| IdField.Instance; TimeStampField.Instance; ModifyIndexField.Instance; StateField.Instance |]
    
    /// Returns all the meta-data schema fields that should be present in an index    
    let getMetaSchemaFields = 
        [| generateSchemaForMetaField IdField.Name IdField.Instance false
           generateSchemaForMetaField TimeStampField.Name TimeStampField.Instance true
           generateSchemaForMetaField ModifyIndexField.Name ModifyIndexField.Instance true
           generateSchemaForMetaField StateField.Name StateField.Instance false |]
    
    /// Returns all the meta-data field templates that should be present in an index    
    let getMetaFieldsTemplates() = 
        [| IdField.Instance.CreateFieldTemplate "" false
           TimeStampField.Instance.CreateFieldTemplate "" true
           ModifyIndexField.Instance.CreateFieldTemplate "" true
           StateField.Instance.CreateFieldTemplate "" false |]
