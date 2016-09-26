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
open Nessos.Streams
open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Text

/// Represents the analyzers associated with a field. By creating this abstraction
/// we can easily create a cache able copy of it which can be shared across field types
type FieldAnalyzers = 
    { SearchAnalyzer    : LuceneAnalyzer
      IndexAnalyzer     : LuceneAnalyzer }

/// Represents the minimum unit to represent a field in FlexSearch Document. The reason
/// to use array is to support fields which can maps to multiple internal fields.
/// Note: We will create a new instance of FieldTemplate per field in an index. So, it
/// should not occupy a lot of memory
type FieldTemplate = 
    { Fields    : LuceneField []
      DocValues : LuceneField [] option }

type FieldProperties = 
    { DataTypeName          : string
      DefaultFieldName      : string option
      SortFieldType         : SortFieldType
      DefaultStringValue    : string
      NeedsExtraStoreField  : bool
      IsNumeric             : bool
      SupportsAnalyzer      : bool
      StoredOnly            : bool
      MinimumStringValue    : string
      MaximumStringValue    : string 
      AutoPopulated         : bool
      CreateField           : string -> LuceneField
      createDvField         : option<string -> LuceneField>
      ToInternalString      : option<string -> string>
      ToExternal            : option<FieldValue -> string> }

type FieldProperties<'T> =
    { Minimum               : 'T
      Maximum               : 'T
      DefaultValue          : 'T
      AddOne                : 'T -> 'T
      SubtractOne           : 'T -> 'T
      RangeQueryGenerator   : SchemaName * 'T * 'T -> Query
      ExactQueryGenerator   : SchemaName * 'T -> Query
      SetQueryGenerator     : SchemaName * 'T[] -> Query
      TryParse              : string -> bool * 'T 
      ToInternal            : option<'T -> 'T>
      UpdateFieldTemplate   : FieldTemplate -> 'T -> unit }

/// FieldBase containing all the Field related properties which are not
/// dependent upon the type information
[<AbstractClass>]
type IndexField(fp : FieldProperties) =
    
    member val DataTypeName         = fp.DataTypeName
    member val DefaultFieldName     = fp.DefaultFieldName
    member val SortFieldType        = fp.SortFieldType
    member val DefaultStringValue   = fp.DefaultStringValue
    member val NeedsExtraStoreField = fp.NeedsExtraStoreField
    member val IsNumeric            = fp.IsNumeric
    member val SupportsAnalyzer     = fp.SupportsAnalyzer
    member val StoredOnly           = fp.StoredOnly
    member val MinimumStringValue   = fp.MinimumStringValue
    member val MaximumStringValue   = fp.MaximumStringValue

    /// Generate any type specific formatting that is needed before sending
    /// the data out as part of search result. This is useful in case of enums
    /// and boolean fields which have a different internal representation. 
    member this.ToExternal = fp.ToExternal
    
    abstract ToInternalString : FieldValue -> string
                        
    /// Create a new Field template for the given field. 
    abstract CreateFieldTemplate : FieldSchema -> FieldTemplate
    
    /// Get tokens for a given input. This is not supported by all field types for example
    /// it does not make any sense to tokenize numeric types and exact text fields. In these
    /// cases the internal representation of the field type is used.
    /// Note: An instance of List is passed so that we can avoid memory allocation and
    /// reuse the list from the object pool.
    /// Note: Avoid using the below for numeric types. 
    abstract GetTokens : FieldValue -> List<string> -> FieldSchema -> unit
        
    /// Update a field template from the given FlexDocument. This is a higher level method which
    /// bring together a number of lower level method from FieldBase
    abstract UpdateDocument : FlexDocument -> SchemaName -> FieldTemplate -> unit
        
    /// Returns a query which provides the exact text match for the given field type.
    abstract ExactQuery : SchemaName -> FieldValue -> Result<Query>
        
    /// Returns a query which provides the range matching over the given lower and
    /// upper range.
    abstract RangeQuery : SchemaName -> LowerRange -> UpperRange -> InclusiveMinimum 
        -> InclusiveMaximum -> Result<Query>
        
    /// Returns a query which matches any of the terms in the given values array.
    abstract SetQuery : SchemaName -> value:string [] -> Result<Query>

/// Represents a field in an Index.
and FieldSchema = 
    { SchemaName    : string
      FieldName     : string
      DocValues     : bool
      Analyzers     : FieldAnalyzers option
      Similarity    : Similarity
      FieldType     : IndexField }

/// KeyedCollection wrapper for Field collections
type FieldCollection() = 
    inherit KeyedCollection<string, FieldSchema>(StringComparer.OrdinalIgnoreCase)
    override __.GetKeyForItem(t : FieldSchema) = t.FieldName
    member this.TryGetValue(key : string) = this.Dictionary.TryGetValue(key)
    member this.ReadOnlyDictionary = new ReadOnlyDictionary<string, FieldSchema>(this.Dictionary)

[<Compile(ModuleSuffix)>]
module FieldTemplate = 
    /// Helpers for creating Lucene field types

    /// A field that is indexed but not tokenized: the entire String value is indexed as a single token. 
    /// For example this might be used for a 'country' field or an 'id' field, or any field that you 
    /// intend to use for sorting or access through the field cache.
    let string fieldName = new StringField(fieldName, Constants.StringDefaultValue, FieldStore.YES) :> LuceneField
        
    let stringDV fieldName = new SortedDocValuesField(fieldName, new FlexLucene.Util.BytesRef()) :> LuceneField
        
    /// A field that is indexed and tokenized, without term vectors. For example this would be used on a 
    /// 'body' field, that contains the bulk of a document's text.
    let text fieldName = new TextField(fieldName, Constants.StringDefaultValue, FieldStore.YES) :> LuceneField
        
    let long fieldName = new LongPoint(fieldName, 0L) :> LuceneField
    let longDV fieldName = new NumericDocValuesField(fieldName, 0L) :> LuceneField
    let int32 fieldName = new IntPoint(fieldName, 0) :> LuceneField
    let int32DV fieldName = new NumericDocValuesField(fieldName, 0L) :> LuceneField
    let double fieldName = new DoublePoint(fieldName, 0.0) :> LuceneField
    let doubleDV fieldName = new DoubleDocValuesField(fieldName, 0.0) :> LuceneField
    let single fieldName = new FloatPoint(fieldName, float32 0.0) :> LuceneField
    let singleDV fieldName = new FloatDocValuesField(fieldName, float32 0.0) :> LuceneField
    let stored fieldName = new StoredField(fieldName, Constants.StringDefaultValue) :> LuceneField
    let binary fieldName = new StoredField(fieldName, [||]) :> LuceneField
    let custom (fieldName, value : string, template : FlexLucene.Document.FieldType) = 
        new LuceneField(fieldName, value, template)
    let bytesForNullString = System.Text.Encoding.Unicode.GetBytes(Constants.StringDefaultValue)

    let setIntValue (ft : FieldTemplate) (value : int) = 
        ft.Fields.[0].SetIntValue(value)
        if ft.DocValues.IsSome then 
            ft.DocValues.Value.[0].SetLongValue(int64 value)
    
    let setLongValue (fieldTemplate : FieldTemplate) (value : int64) = 
        fieldTemplate.Fields.[0].SetLongValue(value)
        if fieldTemplate.DocValues.IsSome then 
            fieldTemplate.DocValues.Value.[0].SetLongValue(int64 value)
    
    let setDoubleValue (fieldTemplate : FieldTemplate) (value : double) = 
        fieldTemplate.Fields.[0].SetDoubleValue(value)
        if fieldTemplate.DocValues.IsSome then 
            fieldTemplate.DocValues.Value.[0].SetDoubleValue(value)
    
    let setSingleValue (fieldTemplate : FieldTemplate) (value : single) = 
        fieldTemplate.Fields.[0].SetFloatValue(value)
        if fieldTemplate.DocValues.IsSome then 
            fieldTemplate.DocValues.Value.[0].SetFloatValue(value)

    let setStringValue (fieldTemplate : FieldTemplate) (value : string) = 
        fieldTemplate.Fields.[0].SetStringValue(value)
        if fieldTemplate.DocValues.IsSome then 
            fieldTemplate.DocValues.Value.[0].SetBytesValue(Encoding.UTF8.GetBytes(value))

    let create (fs : FieldSchema) (createField : string -> LuceneField) 
            (createDvField : option<string -> LuceneField>) = 
            let fields = new ResizeArray<LuceneField>()
            fields.Add(createField fs.SchemaName)
            if fs.FieldType.NeedsExtraStoreField then
                fields.Add(stored fs.SchemaName)
            let docValues = 
                if fs.DocValues && createDvField.IsSome then 
                    Some <| [| createDvField.Value fs.SchemaName |]
                else None
            { Fields = fields.ToArray()
              DocValues = docValues }

/// Information needed to represent a field in FlexSearch document
/// This should only contain information which is fixed for a given type so that the
/// instance could be cached. Any Index specific information should go to FieldSchema                     
type FieldType<'T>(properties : FieldProperties, typeProperties : FieldProperties<'T>) =
    inherit IndexField(properties)

    member val Minimum              = typeProperties.Minimum
    member val Maximum              = typeProperties.Maximum
    member val DefaultValue         = typeProperties.DefaultValue
    member __.AddOne                = typeProperties.AddOne
    member __.SubtractOne           = typeProperties.SubtractOne
    member __.RangeQueryGenerator   = typeProperties.RangeQueryGenerator
    member __.ExactQueryGenerator   = typeProperties.ExactQueryGenerator
    member __.SetQueryGenerator     = typeProperties.SetQueryGenerator
    member __.TryParse              = typeProperties.TryParse

    /// Generates the internal representation of the field. This is mostly
    /// useful when searching if the field does not have an associated analyzer.
    member __.ToInternal            = typeProperties.ToInternal
    
    /// Update a field template with the given value. Call to this
    /// method should be chained from Validate
    member __.UpdateFieldTemplate   = typeProperties.UpdateFieldTemplate

    /// Checks if the Field has a reserved name then returns that otherwise
    /// format's the passed name to the correct schema name 
    member this.GetSchemaName(fieldName : string) = 
        match this.DefaultFieldName with
        | Some name -> name
        | _ -> fieldName

    /// Validates the provided string input to see if it matches the correct format for
    /// field type. In case it can't validate the input then it returns a tuple with
    /// false and the default value of the field. Then it is up to the caller to decide
    /// whether to thrown an error or use the default value for the field.
    member this.Validate (value : string) =
        if isBlank value then
            false, this.DefaultValue 
        else if strEqual value this.DefaultStringValue then
            true, this.DefaultValue
        else 
            if this.IsNumeric then
                if value = this.MaximumStringValue then 
                    true, this.Maximum
                else if value = this.MinimumStringValue then 
                    true, this.Minimum
                else
                    match typeProperties.TryParse value with
                    | true, v -> true, v
                    | false, f -> false, this.DefaultValue
            else
                match typeProperties.TryParse value with
                | true, v -> true, v
                | _ -> false, this.DefaultValue
    
    /// Validate the value and use default field value in case of error.
    member this.ValidateAndContinue (value : string) =
        match this.Validate value with
        | true, v -> v
        | false, defaultValue -> defaultValue

    /// Validate the value and return error property in case of error.
    member this.ValidateAndError (schemaName : string) (value : string) =
        match this.Validate value with
        | true, v -> ok v
        | _ -> fail <| DataCannotBeParsed(schemaName, this.DataTypeName, value)
    
    /// Convert the value to the internal representation
    member this.ToInternalRepresentation (value : 'T) =
        match this.ToInternal with
        | Some(toInternal) -> toInternal value
        | _ -> value    

    override this.ToInternalString (value : string) =
        match properties.ToInternalString with
        | Some(toInternal) -> toInternal value
        | _ -> value    

    override this.CreateFieldTemplate fs = 
        FieldTemplate.create fs properties.CreateField properties.createDvField

    override this.UpdateDocument document schemaName template = 
        match document.Fields.TryGetValue(schemaName) with
        | (true, value) -> value
        | _ -> this.DefaultStringValue
        |> this.ValidateAndContinue
        |> this.ToInternalRepresentation
        |> this.UpdateFieldTemplate template
    
    override this.GetTokens value tokens fs =
        match fs.Analyzers with
        | Some(analyzers) ->
            parseTextUsingAnalyzer (analyzers.SearchAnalyzer, fs.SchemaName, value, tokens)
        | _ -> 
            // The field does not have an associated analyzer so just add the input to
            // the result by using the field specific formatting
            tokens.Add(this.ToInternalString value)

    override this.RangeQuery schemaName lowerRange upperRange includeLower includeUpper = maybe { 
        let! lower = this.ValidateAndError schemaName lowerRange
        let lr = 
            if includeLower then lower
            else this.AddOne lower
        let! upper = this.ValidateAndError schemaName upperRange
        let ur = 
            if includeUpper then upper
            else this.SubtractOne upper
        return this.RangeQueryGenerator(schemaName, lower, upper) }
    
    override this.ExactQuery (schemaName : string) (value : string) = maybe { 
        let! value = this.ValidateAndError schemaName value
        return this.ExactQueryGenerator (schemaName, value) }
    
    override this.SetQuery (schemaName : string) (values : string []) = maybe { 
        let intValues = 
            values
            |> Array.map (fun v -> this.ValidateAndError schemaName v)
            |> Array.filter (fun r -> succeeded r)
            |> Array.map (fun s -> extract s)
        return this.SetQueryGenerator (schemaName, intValues) }
                                                                            
module FieldHelpers =
    let inline add x y = x + y
    let inline subtract y x = x - y

/// Integer 32 bit definition
[<RequireQualifiedAccess>]
module IntField =

    let Info =
        { DataTypeName          = "Int"
          DefaultFieldName      = None
          SortFieldType         = SortFieldType.INT
          IsNumeric             = true
          DefaultStringValue    = "0"
          SupportsAnalyzer      = false
          StoredOnly            = false
          NeedsExtraStoreField  = true
          AutoPopulated         = false
          MinimumStringValue    = JavaIntMin.ToString()
          MaximumStringValue    = JavaIntMax.ToString()
          CreateField           = FieldTemplate.int32
          createDvField         = Some <| FieldTemplate.int32DV
          ToInternalString      = None
          ToExternal            = None }
    
    let TypeInfo =
        { Minimum               = JavaIntMin 
          Maximum               = JavaIntMax
          DefaultValue          = 0
          AddOne                = FieldHelpers.add 1
          SubtractOne           = FieldHelpers.subtract 1
          TryParse              = Int32.TryParse
          RangeQueryGenerator   = IntPoint.NewRangeQuery
          ExactQueryGenerator   = IntPoint.NewExactQuery
          SetQueryGenerator     = IntPoint.NewSetQuery
          ToInternal            = None 
          UpdateFieldTemplate   = FieldTemplate.setIntValue }
    
    let Instance = new FieldType<int32>(Info, TypeInfo) :> IndexField

module LongField =

    /// Integer 64 bit definition
    let Info = 
        { DataTypeName          = "Long"
          DefaultFieldName      = None
          SortFieldType         = SortFieldType.LONG
          IsNumeric             = true
          DefaultStringValue    = "0"
          SupportsAnalyzer      = false
          StoredOnly            = false
          NeedsExtraStoreField  = true
          AutoPopulated         = false
          MinimumStringValue    = JavaLongMin.ToString()
          MaximumStringValue    = JavaLongMax.ToString() 
          CreateField           = FieldTemplate.long
          createDvField         = Some <| FieldTemplate.longDV 
          ToInternalString      = None
          ToExternal            = None }
    
    let TypeInfo =
        { Minimum               = JavaLongMin 
          Maximum               = JavaLongMax
          DefaultValue          = 0L
          AddOne                = FieldHelpers.add 1L
          SubtractOne           = FieldHelpers.subtract 1L
          TryParse              = Int64.TryParse
          RangeQueryGenerator   = LongPoint.NewRangeQuery
          ExactQueryGenerator   = LongPoint.NewExactQuery
          SetQueryGenerator     = LongPoint.NewSetQuery 
          ToInternal            = None 
          UpdateFieldTemplate   = FieldTemplate.setLongValue }

    let Instance = new FieldType<int64>(Info, TypeInfo) :> IndexField

module SingleField =

    /// Float 32 bit definition       
    let Info = 
        { DataTypeName          = "Single"
          DefaultFieldName      = None
          SortFieldType         = SortFieldType.FLOAT
          IsNumeric             = true
          DefaultStringValue    = "0.0"
          SupportsAnalyzer      = false
          StoredOnly            = false
          NeedsExtraStoreField  = true
          AutoPopulated         = false
          MinimumStringValue    = JavaFloatMin.ToString()
          MaximumStringValue    = JavaFloatMax.ToString() 
          CreateField           = FieldTemplate.single
          createDvField         = Some <| FieldTemplate.singleDV 
          ToInternalString      = None
          ToExternal            = None }

    let TypeInfo =
        { Minimum               = JavaFloatMin 
          Maximum               = JavaFloatMax
          DefaultValue          = 0.0f
          AddOne                = FieldHelpers.add 1.0f
          SubtractOne           = FieldHelpers.subtract 1.0f
          TryParse              = Single.TryParse
          RangeQueryGenerator   = FloatPoint.NewRangeQuery
          ExactQueryGenerator   = FloatPoint.NewExactQuery
          SetQueryGenerator     = FloatPoint.NewSetQuery 
          ToInternal            = None
          UpdateFieldTemplate   = FieldTemplate.setSingleValue }

    let Instance = new FieldType<float32>(Info, TypeInfo) :> IndexField

module DoubleField =

    /// Float 64 bit definition                    
    let Info = 
        { DataTypeName          = "Double"
          DefaultFieldName      = None
          SortFieldType         = SortFieldType.DOUBLE
          IsNumeric             = true
          DefaultStringValue    = "0.0"
          SupportsAnalyzer      = false
          StoredOnly            = false
          NeedsExtraStoreField  = true
          AutoPopulated         = false
          MinimumStringValue    = JavaDoubleMin.ToString()
          MaximumStringValue    = JavaDoubleMax.ToString()
          CreateField           = FieldTemplate.double
          createDvField         = Some <| FieldTemplate.doubleDV 
          ToInternalString      = None
          ToExternal            = None }

    let TypeInfo =
        { Minimum               = JavaDoubleMin 
          Maximum               = JavaDoubleMax
          DefaultValue          = 0.0
          AddOne                = FieldHelpers.add 1.0
          SubtractOne           = FieldHelpers.subtract 1.0
          TryParse              = Double.TryParse
          RangeQueryGenerator   = DoublePoint.NewRangeQuery
          ExactQueryGenerator   = DoublePoint.NewExactQuery
          SetQueryGenerator     = DoublePoint.NewSetQuery 
          ToInternal            = None
          UpdateFieldTemplate   = FieldTemplate.setDoubleValue }

    let Instance = new FieldType<double>(Info, TypeInfo) :> IndexField

module DateTimeField =

    /// Datetime definition
    let Info = 
        { LongField.Info with
              DataTypeName          = "DateTime"
              DefaultStringValue    = "00010101000000" // Equivalent to 00:00:00.0000000, January 1, 0001, in the Gregorian calendar
              MinimumStringValue    = "00010101000000"
              MaximumStringValue    = "99991231235959" }

    let TypeInfo =
        { LongField.TypeInfo with
              Minimum               = 00010101000000L 
              Maximum               = 99991231235959L
              DefaultValue          = 00010101000000L }

    let Instance = new FieldType<int64>(Info, TypeInfo) :> IndexField

module DateField =

    /// Date definition
    let Info = 
        { LongField.Info with
              DataTypeName          = "Date"
              DefaultStringValue    = "00010101"
              MinimumStringValue    = "00010101"
              MaximumStringValue    = "99991231" }

    let TypeInfo =
        { LongField.TypeInfo with
              Minimum               = 00010101L 
              Maximum               = 99991231L
              DefaultValue          = 00010101L }

    let Instance = new FieldType<int64>(Info, TypeInfo) :> IndexField

module KeywordField =

    /// Parse a string. Matches the signature of other parse methods
    let stringTryParse (value : string) =
        if String.IsNullOrWhiteSpace(value) then
            (false, "null")
        else
            (true, value)
    
    let termRangeQuery (schemaName, lowerRange : string, upperRange : string) =
        new TermRangeQuery(schemaName, 
                           new FlexLucene.Util.BytesRef(Encoding.UTF8.GetBytes lowerRange), 
                           new FlexLucene.Util.BytesRef(Encoding.UTF8.GetBytes upperRange), true, true) 
                           :> Query
    
    let exactQuery (schemaName, value : string) =
        Query.termQuery schemaName value
    
    let queryWrapper (schemaName, value : string[]) =
        failwithf "Unsupported query"

    /// Keyword definition                    
    let Info = 
        { DataTypeName          = "Keyword"
          DefaultFieldName      = None
          SortFieldType         = SortFieldType.STRING
          IsNumeric             = false
          DefaultStringValue    = "null"
          SupportsAnalyzer      = false
          StoredOnly            = false
          NeedsExtraStoreField  = false
          AutoPopulated         = false
          MinimumStringValue    = ""
          MaximumStringValue    = ""
          CreateField           = FieldTemplate.string
          createDvField         = Some <| FieldTemplate.stringDV
          ToInternalString      = Some <| fun x -> x.ToLowerInvariant() 
          ToExternal            = None }

    let TypeInfo =
        { Minimum               = "" 
          Maximum               = ""
          DefaultValue          = "null"
          AddOne                = fun x -> x
          SubtractOne           = fun x -> x
          TryParse              = stringTryParse
          RangeQueryGenerator   = termRangeQuery
          ExactQueryGenerator   = exactQuery
          SetQueryGenerator     = queryWrapper 
          ToInternal            = Some <| fun x -> x.ToLowerInvariant() 
          UpdateFieldTemplate   = FieldTemplate.setStringValue }

    let Instance = new FieldType<string>(Info, TypeInfo) :> IndexField

module TextField =

    /// Text definition                    
    let Info = 
        { KeywordField.Info with
              DataTypeName          = "Text"
              SortFieldType         = SortFieldType.STRING
              SupportsAnalyzer      = true
              CreateField           = FieldTemplate.text
              createDvField         = None }
    
    let TypeInfo =
        { KeywordField.TypeInfo with
            ToInternal = None }

    let Instance = new FieldType<string>(Info, TypeInfo) :> IndexField

module BoolField =
    let trueString = "true"
    let falseString = "false"
    let trueInternal = "t"
    let falseInternal = "f"

    let inline toExternal (value : string) = 
        if strEqual value trueInternal  then trueString
        else falseString

    let inline toInternal (value : string) =
        if value |> strStartsWith trueInternal then 
            trueInternal
         else
            falseInternal
    
    /// Bool field definition                    
    let Info = 
        { KeywordField.Info with
              DataTypeName          = "Bool"
              DefaultStringValue    = "f"
              MinimumStringValue    = "f"
              MaximumStringValue    = "t"
              CreateField           = FieldTemplate.text
              createDvField         = None 
              ToInternalString      = Some <| toInternal
              ToExternal            = Some <| toExternal }

    let TypeInfo =
        { KeywordField.TypeInfo with
            Minimum         = "f" 
            Maximum         = "t"
            DefaultValue    = "f"
            ToInternal      = Some <| toInternal }
             
    let Instance = new FieldType<string>(Info, TypeInfo) :> IndexField

module StoredField =

    let Info =
        { DataTypeName          = "Stored"
          DefaultFieldName      = None
          SortFieldType         = SortFieldType.SCORE
          IsNumeric             = false
          DefaultStringValue    = ""
          SupportsAnalyzer      = false
          StoredOnly            = true
          NeedsExtraStoreField  = false
          AutoPopulated         = false
          MinimumStringValue    = ""
          MaximumStringValue    = ""
          CreateField           = FieldTemplate.stored
          createDvField         = None
          ToInternalString      = None
          ToExternal            = None }
    
    let TypeInfo =
        { Minimum               = "" 
          Maximum               = ""
          DefaultValue          = ""
          AddOne                = fun x -> x
          SubtractOne           = fun x -> x
          TryParse              = KeywordField.stringTryParse
          RangeQueryGenerator   = fun _ -> failwithf "Internal error: Stored field does not support searching."
          ExactQueryGenerator   = fun _ -> failwithf "Internal error: Stored field does not support searching."
          SetQueryGenerator     = fun _ -> failwithf "Internal error: Stored field does not support searching."
          ToInternal            = None 
          UpdateFieldTemplate   = FieldTemplate.setStringValue }
    
    let Instance = new FieldType<string>(Info, TypeInfo) :> IndexField

[<Compile(ModuleSuffix)>]
module Field = 
                                         
    let getFieldType (field : FlexField) = 
        match field.FieldType with
        | FieldType.Int         -> IntField.Instance
        | FieldType.Long        -> LongField.Instance
        | FieldType.Double      -> DoubleField.Instance 
        | FieldType.Float       -> SingleField.Instance
        | FieldType.Date        -> DateField.Instance
        | FieldType.DateTime    -> DateTimeField.Instance        
        | FieldType.Bool        -> BoolField.Instance
        | FieldType.Keyword     -> KeywordField.Instance
        | FieldType.Text        -> TextField.Instance
        | FieldType.Stored      -> StoredField.Instance
        | _ -> failwithf "Internal error: Unsupported FieldType"

module IdField =
    let Name = "_id"

    let Info = { KeywordField.Info with
                    DefaultFieldName = Some Name }

    /// Used for representing the id of an index
    let Instance = new FieldType<string>(Info, KeywordField.TypeInfo) :> IndexField

/// This field is used to add causal ordering to the events in 
/// the index. A document with lower modify index was created/updated before
/// a document with the higher index.
/// This is also used for concurrency updates.
module ModifyIndexField =
    let Name = "_modifyindex"

    let Info = { LongField.Info with
                    DefaultFieldName = Some Name }

    let Instance = new FieldType<int64>(Info, LongField.TypeInfo) :> IndexField

    
/// Field to be used for time stamp. This field is used by index to capture the modification
/// time.
/// It only supports a fixed YYYYMMDDHHMMSSfff format.
module TimeStampField =
    let Name = "_timestamp"

    let Info = { LongField.Info with
                    AutoPopulated    = true
                    DefaultFieldName = Some Name }

    let inline toInternal (value : 'T) =
        // The timestamp value will always be auto generated
        GetCurrentTimeAsLong()
    
    let TypeInfo = { LongField.TypeInfo with
                        ToInternal = Some <| toInternal }

    let Instance = new FieldType<int64>(Info, TypeInfo) :> IndexField

[<Compile(ModuleSuffix)>]
module FieldSchema = 
    
    /// Method to map boolean to FieldStore enum
    let store (isStored : bool) (schema : FieldSchema) = 
        if isStored then FieldStore.YES
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
    let isSearchable (schema : FieldSchema) = not schema.FieldType.StoredOnly

    /// Signifies if the field supports doc values
    let hasDocValues (schema : FieldSchema) = schema.DocValues

    /// Signifies if the field allows sorting
    let allowSorting (schema : FieldSchema) = schema |> hasDocValues
    
    /// Signifies if the field is numeric
    let isNumericField (schema : FieldSchema) = schema.FieldType.IsNumeric

    /// Build a Schema field from the Field DTO
    let build (field : FlexField) (getAnalyzer : GetAnalyzer) = 
               
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
            let basicFieldType = Field.getFieldType field
            let! analyzers = getAnalyzers field
            return { FieldName  = field.FieldName
                     SchemaName = field.FieldName
                     DocValues  = field.AllowSort
                     FieldType  = basicFieldType
                     Similarity = field.Similarity
                     Analyzers  = analyzers }
        }

    ///----------------------------------------------------------------------
    /// Meta data fields related
    ///----------------------------------------------------------------------
    /// Helper method to generate FieldSchema for a given meta data field
    let generateSchemaForMetaField (fieldDef : IndexField) docValues = 
        { FieldName     = fieldDef.DefaultFieldName.Value
          SchemaName    = fieldDef.DefaultFieldName.Value
          DocValues     = docValues
          FieldType     = fieldDef
          Similarity    = FlexSearch.Api.Constants.Similarity.BM25
          Analyzers     = None }
    
    /// List of all meta-data fields in the system
    let metaDataFields = [| IdField.Name; ModifyIndexField.Name; TimeStampField.Name |]

    /// Returns all the meta-data fields that should be present in an index    
    let getMetaFields = [| IdField.Instance; ModifyIndexField.Instance; TimeStampField.Instance |]
    
    /// Returns all the meta-data schema fields that should be present in an index    
    let getMetaSchemaFields = 
        [| generateSchemaForMetaField IdField.Instance false
           generateSchemaForMetaField ModifyIndexField.Instance true 
           generateSchemaForMetaField TimeStampField.Instance true|]
    
    /// Returns all the meta-data field templates that should be present in an index    
    let getMetaFieldsTemplates() = 
        getMetaSchemaFields
        |> Array.map(fun fs -> fs.FieldType.CreateFieldTemplate fs)


//    /// FieldBase containing all the Field related properties which are not
//    /// dependent upon the type information
//    [<AbstractClass>]
//    type FieldBase(sortFieldType, defaultFieldName : string option) = 
//        
//        /// Checks if the Field has a reserved name then returns that otherwise
//        /// format's the passed name to the correct schema name 
//        member __.GetSchemaName(fieldName : string) = 
//            match defaultFieldName with
//            | Some name -> name
//            | _ -> fieldName
//        
//        //member __.LuceneFieldType : FieldType = luceneFieldType
//        member __.SortFieldType : SortFieldType = sortFieldType
//        abstract DefaultStringValue : string
//        
//        /// Generate any type specific formatting that is needed before sending
//        /// the data out as part of search result. This is useful in case of enums
//        /// and boolean fields which have a different internal representation. 
//        abstract ToExternal : option<FieldValue -> string>
//        
//        /// Default implementation of ToExternal as most of the types will not have
//        /// any specific external formatting rules
//        override __.ToExternal = None
//        
//        /// Generates the internal representation of the field. This is mostly
//        /// useful when searching if the field does not have an associated analyzer.
//        abstract ToInternal : option<string -> string>
//        
//        override __.ToInternal = None
//        
//        /// Create a new Field template for the given field. 
//        abstract CreateFieldTemplate : SchemaName -> generateDocValues:bool -> FieldTemplate
//        
//        /// Update a field template from the given FlexDocument. This is a higher level method which
//        /// bring together a number of lower level method from FieldBase
//        abstract UpdateDocument : FlexDocument -> SchemaName -> FieldTemplate -> unit
//        
//        /// Returns a query which provides the exact text match for the given field type.
//        abstract ExactQuery : SchemaName -> FieldValue -> Result<Query>
//        
//        /// Returns a query which provides the range matching over the given lower and
//        /// upper range.
//        abstract RangeQuery : SchemaName
//         -> LowerRange -> UpperRange -> includeLower:bool -> includeUpper:bool -> Result<Query>
//        
//        /// Returns a query which matches any of the terms in the given values array.
//        abstract SetQuery : SchemaName -> value:string [] -> Result<Query>
//        
//        //    /// Get a range query for the given type
//        //    abstract GetRangeQuery : SchemaName
//        //     -> LowerRange -> UpperRange -> InclusiveMinimum -> InclusiveMaximum -> Result<Query>
//        //    
//        //    /// A specialized case of range query which is useful when both upper and lower range 
//        //    /// are equal
//        //    member this.GetNumericQuery schemaName lowerRange = this.GetRangeQuery schemaName lowerRange lowerRange true true
//        //    
//        member this.GetNumericBooleanQuery schemaName (values : string []) (occur : BooleanClauseOccur) = 
//            assert (values.Length <> 0)
//            maybe { 
//                if values.Length = 1 then return! this.ExactQuery schemaName values.[0]
//                else return! this.SetQuery schemaName values
//            }
//    
//    type RangeQueryGenerator<'T> = SchemaName * 'T * 'T -> Query
//    
//    /// Information needed to represent a field in FlexSearch document
//    /// This should only contain information which is fixed for a given type so that the
//    /// instance could be cached. Any Index specific information should go to FieldSchema
//    [<AbstractClass>]
//    type FieldBase<'T>(luceneFieldType, sortFieldType, defaultValue, minimumValue, maximumValue, defaultFieldName : string option) = 
//        inherit FieldBase(sortFieldType, defaultFieldName)
//        member __.DefaultValue : 'T = defaultValue
//        override __.DefaultStringValue = defaultValue.ToString()
//        
//        /// The minimum value supported by the Field type 
//        member __.Minimum : 'T = minimumValue
//        
//        /// Minimum value in string. Used for comparison
//        member __.MinimumStringValue = minimumValue.ToString()
//        
//        /// The maximum value supported by the Field type 
//        member __.Maximum : 'T = maximumValue
//        
//        /// Maximum value in string. Used for comparison
//        member this.MaximumStringValue = maximumValue.ToString()
//        
//        /// Add numeric one to the field value 
//        abstract AddOne : 'T -> 'T
//        
//        /// Subtract numeric one to the field value 
//        abstract SubtractOne : 'T -> 'T
//        
//        abstract DataTypeName : string
//        abstract RangeQueryGenerator : SchemaName * 'T * 'T -> Query
//        abstract TryParse : string -> bool * 'T
//        
//        member this.ParseNumber schemaName (number : string) = 
//            if number = this.MaximumStringValue then ok this.Maximum
//            else if number = this.MinimumStringValue then ok this.Minimum
//            else 
//                match this.TryParse number with
//                | true, v -> ok v
//                | _ -> fail <| DataCannotBeParsed(schemaName, this.DataTypeName, number)
//        
//        /// Update a field template with the given value. Call to this
//        /// method should be chained from Validate
//        abstract UpdateFieldTemplate : FieldTemplate -> 'T -> unit
//        
//        override this.UpdateDocument document schemaName template = 
//            match document.Fields.TryGetValue(schemaName) with
//            | (true, value) -> value
//            | _ -> this.DefaultStringValue
//            |> this.Validate
//            |> this.UpdateFieldTemplate template
//        
//        /// Validate the given string for the Field. This works in
//        /// conjunction with the UpdateFieldTemplate
//        abstract Validate : value:string -> 'T
//        
//        override this.RangeQuery (schemaName : string) (lowerRange : string) (upperRange : string) (includeLower : bool) 
//                 (includeUpper : bool) = 
//            maybe { 
//                let! lower = this.ParseNumber schemaName lowerRange
//                let lr = 
//                    if includeLower then lower
//                    else this.AddOne lower
//                let! upper = this.ParseNumber schemaName upperRange
//                let ur = 
//                    if includeUpper then upper
//                    else this.SubtractOne upper
//                return this.RangeQueryGenerator(schemaName, lower, upper)
//            }
//        
//        let exactQuery (schemaName : string) (value : string) (queryGenerator : ExactQueryGenerator<'T>) = 
//            maybe { let! value = parseMethod schemaName value rangeLimiter
//                    return queryGenerator (schemaName, value) }
//    
//    type SetQueryGenerator<'T> = SchemaName * 'T [] -> Query
//    
//    let setQuery (schemaName : string) (values : string []) (parseMethod : ParseMethod<'T>) (rangeLimiter : 'T) 
//        (queryGenerator : SetQueryGenerator<'T>) = 
//        maybe { 
//            let intValues = 
//                values
//                |> Array.map (fun v -> parseMethod schemaName v rangeLimiter)
//                |> Array.filter (fun r -> succeeded r)
//                |> Array.map (fun s -> extract s)
//            return queryGenerator (schemaName, intValues)
//        }
//    
//
//    
//    module NumericHelpers = 
//        let fieldType (dimensions : int) (length : int) = 
//            let t = new FieldType()
//            t.SetDimensions(dimensions, length)
//            t.Freeze()
//            t
//        
//        type ParseMethod<'T> = SchemaName -> string -> 'T -> Result<'T>
//        
//        type QueryGenerator<'T> = SchemaName * 'T * 'T -> Query
//        
//        let createFieldTemplate (schemaName : string) (generateDV : bool) (createField : string -> LuceneField) 
//            (createDvField : string -> LuceneField) = 
//            { Fields = [| createField schemaName |]
//              DocValues = 
//                  if generateDV then Some <| [| createDvField schemaName |]
//                  else None }
//    
//    /// Field that indexes integer values for efficient range filtering and sorting.
//    type IntField() = 
//        inherit FieldBase<int32>(IntField.FieldType, SortFieldType.INT, 0, None)
//        static member FieldType = NumericHelpers.fieldType 1 java.lang.Integer.BYTES
//        static member Instance = new IntField() :> FieldBase
//        override this.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
//            NumericHelpers.createFieldTemplate (this.GetSchemaName schemaName) generateDV CreateField.int 
//                CreateField.intDV
//        override __.UpdateFieldTemplate (template : FieldTemplate) (value : int) = 
//            template |> FieldTemplate.setIntValue value
//        override this.Validate(value : string) = pInt this.DefaultValue value
//        override this.ExactQuery (schemaName : string) (value : string) = 
//            NumericHelpers.exactQuery schemaName value parseInt JavaIntMin IntPoint.NewExactQuery
//        override this.SetQuery (schemaName : string) values = 
//            NumericHelpers.setQuery schemaName values parseInt JavaIntMin IntPoint.NewSetQuery
//    
//    /// Field that indexes double values for efficient range filtering and sorting.
//    type DoubleField() = 
//        inherit FieldBase<double>(DoubleField.FieldType, SortFieldType.DOUBLE, 0.0, None)
//        static member Instance = new DoubleField() :> FieldBase
//        static member FieldType = NumericHelpers.fieldType 1 java.lang.Double.BYTES
//        override this.Validate(value : string) = pDouble this.DefaultValue value
//        override this.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
//            NumericHelpers.createFieldTemplate (this.GetSchemaName schemaName) generateDV CreateField.double 
//                CreateField.doubleDV
//        override this.UpdateFieldTemplate (template : FieldTemplate) (value : double) = 
//            template |> FieldTemplate.setDoubleValue value
//        override this.ExactQuery (schemaName : string) (value : string) = 
//            NumericHelpers.exactQuery schemaName value parseDouble JavaDoubleMin DoublePoint.NewExactQuery
//        override this.SetQuery (schemaName : string) (values : string []) = 
//            NumericHelpers.setQuery schemaName values parseDouble JavaDoubleMin DoublePoint.NewSetQuery
//    
//    /// Field that indexes float values for efficient range filtering and sorting.
//    type FloatField() = 
//        inherit FieldBase<single>(FloatField.FieldType, SortFieldType.FLOAT, 0.0f, None)
//        static member Instance = new FloatField() :> FieldBase
//        static member FieldType = NumericHelpers.fieldType 1 java.lang.Float.BYTES
//        override this.Validate(value : string) = pFloat this.DefaultValue value
//        override this.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
//            NumericHelpers.createFieldTemplate (this.GetSchemaName schemaName) generateDV CreateField.float 
//                CreateField.floatDV
//        override this.UpdateFieldTemplate (template : FieldTemplate) (value : single) = 
//            template |> FieldTemplate.setSingleValue value
//        override this.ExactQuery (schemaName : string) (value : string) = 
//            NumericHelpers.exactQuery schemaName value parseFloat JavaFloatMin FloatPoint.NewExactQuery
//        override this.SetQuery (schemaName : string) (values : string []) = 
//            NumericHelpers.setQuery schemaName values parseFloat JavaFloatMin FloatPoint.NewSetQuery
//    
//    /// Field that indexes long values for efficient range filtering and sorting.
//    type LongField(defaultValue : int64, ?defaultFieldName) = 
//        inherit FieldBase<int64>(LongField.FieldType, SortFieldType.LONG, defaultValue, defaultFieldName)
//        static member Instance = new FloatField() :> FieldBase
//        static member FieldType = NumericHelpers.fieldType 1 java.lang.Long.BYTES
//        override this.Validate(value : string) = pLong this.DefaultValue value
//        override this.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
//            NumericHelpers.createFieldTemplate (this.GetSchemaName schemaName) generateDV CreateField.long 
//                CreateField.longDV
//        override this.UpdateFieldTemplate (template : FieldTemplate) (value : int64) = 
//            template |> FieldTemplate.setLongValue value
//        override this.ExactQuery (schemaName : string) (value : string) = 
//            NumericHelpers.exactQuery schemaName value parseLong JavaLongMin LongPoint.NewExactQuery
//        override this.SetQuery (schemaName : string) (values : string []) = 
//            NumericHelpers.setQuery schemaName values parseLong JavaLongMin LongPoint.NewSetQuery
//    
//    /// Field that indexes date time values for efficient range filtering and sorting.
//    /// It only supports a fixed YYYYMMDDHHMMSS format.
//    type DateTimeField() as self = 
//        inherit LongField(00010101000000L) // Equivalent to 00:00:00.0000000, January 1, 0001, in the Gregorian calendar
//        static member Instance = new DateTimeField() :> FieldBase
//        override __.Validate(value : string) = 
//            // TODO: Implement custom validation for date time
//            pLong self.DefaultValue value
//    
//    /// Field that indexes date time values for efficient range filtering and sorting.
//    /// It only supports a fixed YYYYMMDD format.
//    type DateField() as self = 
//        inherit LongField(00010101L) // Equivalent to January 1, 0001, in the Gregorian calendar
//        static member Instance = new DateField() :> FieldBase
//        override __.Validate(value : string) = 
//            // TODO: Implement custom validation for date
//            pLong self.DefaultValue value
//    
//    /// A field that is indexed and tokenized, without term vectors. For example this would be used on 
//    /// a 'body' field, that contains the bulk of a document's text.
//    /// Note: This field does not support sorting.
//    type TextField() as self = 
//        inherit FieldBase<string>(TextField.TYPE_STORED, SortFieldType.LONG, "null", None)
//        static member Instance = new TextField() :> FieldBase
//        
//        override this.Validate(value : string) = 
//            if String.IsNullOrWhiteSpace value then this.DefaultValue
//            else value
//        
//        override __.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
//            { Fields = [| CreateField.text <| self.GetSchemaName schemaName |]
//              DocValues = None }
//        
//        override this.UpdateFieldTemplate (template : FieldTemplate) (value : string) = 
//            template.Fields.[0].SetStringValue(value)
//        override this.RangeQuery (schemaName : string) (lowerRange : string) (upperRange : string) inludeLower 
//                 includeUpper = 
//            new TermRangeQuery(schemaName, new FlexLucene.Util.BytesRef(Encoding.UTF8.GetBytes lowerRange), 
//                               new FlexLucene.Util.BytesRef(Encoding.UTF8.GetBytes upperRange), true, true) :> Query 
//            |> Ok
//        override this.ExactQuery (schemaName : string) (value : string) = Query.termQuery schemaName value |> Ok
//        override this.SetQuery (schemaName : string) (values : string []) = 
//            failwithf "Field type: Text does not support set query."
//    
//    /// A field that is indexed but not tokenized: the entire String value is indexed as a single token. 
//    /// For example this might be used for a 'country' field or an 'id' field, or any field that you 
//    /// intend to use for sorting or access through the field cache.
//    type ExactTextField(?defaultFieldName) as self = 
//        inherit FieldBase<String>(StringField.TYPE_STORED, SortFieldType.STRING, "null", defaultFieldName)
//        static member Instance = new ExactTextField() :> FieldBase
//        
//        override this.Validate(value : string) = 
//            if String.IsNullOrWhiteSpace value then this.DefaultStringValue
//            else value.ToLowerInvariant() // ToLower is necessary to make searching case insensitive
//        
//        override __.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
//            { Fields = [| CreateField.string <| self.GetSchemaName schemaName |]
//              DocValues = 
//                  if generateDV then Some <| [| CreateField.stringDV <| self.GetSchemaName schemaName |]
//                  else None }
//        
//        override this.UpdateFieldTemplate (template : FieldTemplate) (value : string) = 
//            template.Fields.[0].SetStringValue(value)
//            if template.DocValues.IsSome then template.DocValues.Value.[0].SetBytesValue(Encoding.UTF8.GetBytes(value))
//        
//        override this.RangeQuery (schemaName : string) (lowerRange : string) (upperRange : string) inludeLower 
//                 includeUpper = 
//            new TermRangeQuery(schemaName, new FlexLucene.Util.BytesRef(Encoding.UTF8.GetBytes lowerRange), 
//                               new FlexLucene.Util.BytesRef(Encoding.UTF8.GetBytes upperRange), true, true) :> Query 
//            |> Ok
//        override this.ExactQuery (schemaName : string) (value : string) = Query.termQuery schemaName value |> Ok
//        override this.SetQuery (schemaName : string) (values : string []) = 
//            failwithf "Field type: Keyword does not support set query."
//    
//    /// Field that indexes boolean values.
//    type BoolField() = 
//        inherit ExactTextField()
//        let trueString = "true"
//        let falseString = "false"
//        
//        let toExternal (value : string) = 
//            if String.Equals("t", value, StringComparison.InvariantCultureIgnoreCase) then trueString
//            else falseString
//        
//        static member Instance = new BoolField() :> FieldBase
//        
//        override this.Validate(value : string) = 
//            if String.IsNullOrWhiteSpace value then this.DefaultStringValue
//            else if value.StartsWith("t", true, Globalization.CultureInfo.InvariantCulture) then "t"
//            else "f"
//        
//        override __.ToExternal = Some <| toExternal
//    
//    /// A field which is only stored and is not search-able
//    type StoredField() as self = 
//        inherit FieldBase<string>(StoredField.TYPE, SortFieldType.SCORE, "null", None)
//        static member Instance = new StoredField() :> FieldBase
//        
//        override this.Validate(value : string) = 
//            if String.IsNullOrWhiteSpace value then this.DefaultValue
//            else value
//        
//        override __.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
//            { Fields = [| CreateField.text <| self.GetSchemaName schemaName |]
//              DocValues = None }
//        
//        override this.UpdateFieldTemplate (template : FieldTemplate) (value : string) = 
//            template.Fields.[0].SetStringValue(value)
//        override this.RangeQuery (schemaName : string) (lowerRange : string) (upperRange : string) inludeLower 
//                 includeUpper = failwithf "Field type: Stored does not support range query."
//        override this.ExactQuery (schemaName : string) (value : string) = 
//            failwithf "Field type: Stored does not support exact query."
//        override this.SetQuery (schemaName : string) (values : string []) = 
//            failwithf "Field type: Stored does not support set query."
//    
//    /// ----------------------------------------------------------------------
//    /// Extended field types
//    /// ----------------------------------------------------------------------
//    /// Field to be used for time stamp. This field is used by index to capture the modification
//    /// time.
//    /// It only supports a fixed YYYYMMDDHHMMSSfff format.
//    type TimeStampField() = 
//        inherit LongField(00010101000000000L, TimeStampField.Name)
//        static do addToMetaFields TimeStampField.Name
//        static member Name = "_timestamp"
//        static member Instance = new TimeStampField() :> FieldBase
//        override this.UpdateDocument document schemaName template = 
//            // The timestamp value will always be auto generated
//            this.UpdateFieldTemplate template (GetCurrentTimeAsLong())
//    
//    /// Used for representing the id of an index
//    type IdField() = 
//        inherit ExactTextField(IdField.Name)
//        static do addToMetaFields IdField.Name
//        static member Name = "_id"
//        static member Instance = new IdField() :> FieldBase
//        override this.UpdateDocument document schemaName template = 
//            this.Validate document.Id |> this.UpdateFieldTemplate template
//    
//    /// Used for representing the id of an index
//    type StateField() = 
//        inherit ExactTextField(StateField.Name)
//        static do addToMetaFields StateField.Name
//        static member Name = "_state"
//        static member Instance = new StateField() :> FieldBase
//        static member Active = "active"
//        static member Inactive = "inactive"
//        override this.UpdateDocument document schemaName template = 
//            // Set it to Active for all normal indexing requests
//            this.UpdateFieldTemplate template StateField.Active
//        /// Helper method to set the state to Inactive
//        member this.UpdateFieldToInactive template = this.UpdateFieldTemplate template StateField.Inactive
//    
//    /// This field is used to add causal ordering to the events in 
//    /// the index. A document with lower modify index was created/updated before
//    /// a document with the higher index.
//    /// This is also used for concurrency updates.
//    type ModifyIndexField() = 
//        inherit LongField(0L, ModifyIndexField.Name)
//        static do addToMetaFields ModifyIndexField.Name
//        static member Name = "_modifyindex"
//        static member Instance = new ModifyIndexField() :> FieldBase
//        override this.UpdateDocument document schemaName template = 
//            this.UpdateFieldTemplate template document.ModifyIndex
