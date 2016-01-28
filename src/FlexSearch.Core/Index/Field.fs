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

/// Represents the analyzers associated with a field. By creating this abstraction
/// we can easily create a cache able copy of it which can be shared across field types
type FieldAnalyzers = 
    { SearchAnalyzer : LuceneAnalyzer
      IndexAnalyzer : LuceneAnalyzer }

/// Represents the minimum unit to represent a field in FlexSearch Document. The reason
/// to use array is to support fields which can maps to multiple internal fields.
/// Note: We will create a new instance of FieldTemplate per field in an index. So, it
/// should not occupy a lot of memory
type FieldTemplate = 
    { Fields : LuceneField []
      DocValues : LuceneField [] option }

type RangeQueryProperties<'T> = 
    { Minimum : 'T
      Maxmimum : 'T
      InclusiveMinimum : bool
      InclusiveMaximum : bool }

/// FieldBase containing all the Field related properties which are not
/// dependent upon the type information
[<AbstractClass>]
type FieldBase(luceneFieldType, sortFieldType, defaultFieldName : string option) = 
    
    /// Checks if the Field has a reserved name then returns that otherwise
    /// format's the passed name to the correct schema name 
    member __.GetSchemaName(fieldName : string) = 
        match defaultFieldName with
        | Some name -> name
        | _ -> fieldName
    
    member __.LuceneFieldType : FieldType = luceneFieldType
    member __.SortFieldType : SortFieldType = sortFieldType
    abstract DefaultStringValue : string
    
    /// Generate any type specific formatting that is needed before sending
    /// the data out as part of search result. This is useful in case of enums
    /// and boolean fields which have a different internal representation. 
    abstract ToExternal : option<FieldValue -> string>
    
    /// Default implementation of ToExternal as most of the types will not have
    /// any specific external formatting rules
    override __.ToExternal = None
    
    /// Create a new Field template for the given field. 
    abstract CreateFieldTemplate : SchemaName -> generateDocValues:bool -> FieldTemplate
    
    /// Update a field template from the given FlexDocument. This is a higher level method which
    /// bring together a number of lower level method from FieldBase
    abstract UpdateDocument : FlexDocument -> SchemaName -> FieldSource option -> FieldTemplate -> unit
    
    /// Get a range query for the given type
    abstract GetRangeQuery : option<SchemaName -> (LowerRange * UpperRange) -> (InclusiveMinimum * InclusiveMaximum) -> Result<Query>>
    
    /// Get tokens for a given input. This is not supported by all field types for example
    /// it does not make any sense to tokenize numeric types and exact text fields
    abstract GetTokens : option<string -> option<LuceneAnalyzer> -> List<string>>
    
    override __.GetTokens = None

/// Information needed to represent a field in FlexSearch document
/// This should only contain information which is fixed for a given type so that the
/// instance could be cached. Any Index specific information should go to FieldSchema
[<AbstractClass>]
type FieldBase<'T>(luceneFieldType, sortFieldType, defaultValue, defaultFieldName : string option) = 
    inherit FieldBase(luceneFieldType, sortFieldType, defaultFieldName)
    member __.DefaultValue : 'T = defaultValue
    override __.DefaultStringValue = defaultValue.ToString()
    
    /// Update a field template with the given value. Call to this
    /// method should be chained from Validate
    abstract UpdateFieldTemplate : FieldTemplate -> 'T -> unit
    
    override this.UpdateDocument document schemaName fieldSource template = 
        // If it is computed field then generate and add it otherwise follow standard path
        match fieldSource with
        | Some(s, options) -> 
            try 
                // Wrong values for the data type will still be handled as update Lucene field will
                // check the data type
                let value = s.Invoke(document.IndexName, schemaName, document.Fields, options)
                value
            with _ -> this.DefaultStringValue
        | None -> 
            match document.Fields.TryGetValue(schemaName) with
            | (true, value) -> value
            | _ -> this.DefaultStringValue
        |> this.Validate
        |> this.UpdateFieldTemplate template
    
    // Validate the given string for the Field. This works in
    // conjunction with the UpdateFieldTemplate
    abstract Validate : value:string -> 'T

/// Helpers for creating Lucene field types
[<RequireQualifiedAccess>]
module CreateField = 
    /// A field that is indexed but not tokenized: the entire String value is indexed as a single token. 
    /// For example this might be used for a 'country' field or an 'id' field, or any field that you 
    /// intend to use for sorting or access through the field cache.
    let string fieldName = new StringField(fieldName, Constants.StringDefaultValue, FieldStore.YES) :> LuceneField
    
    let stringDV fieldName = new SortedDocValuesField(fieldName, new FlexLucene.Util.BytesRef()) :> LuceneField
    
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

/// Field that indexes integer values for efficient range filtering and sorting.
type IntField() = 
    inherit FieldBase<int32>(IntField.TYPE_STORED, SortFieldType.INT, 0, None)
    let getRangeQuery schemaName (lowerRange, upperRange) (inclusiveMinimum, inclusiveMaximum) = 
        maybe 
            { 
            let! lower = parseNumber<int32, java.lang.Integer> (schemaName, "Integer") lowerRange JavaIntMin 
                             Int32.TryParse javaInt
            let! upper = parseNumber<int32, java.lang.Integer> (schemaName, "Integer") upperRange JavaIntMin 
                             Int32.TryParse javaInt
            return NumericRangeQuery.NewIntRange(schemaName, lower, upper, inclusiveMinimum, inclusiveMaximum) :> Query }
    static member Instance = new IntField() :> FieldBase
    
    override this.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
        { Fields = [| CreateField.int <| this.GetSchemaName schemaName |]
          DocValues = 
              if generateDV then Some <| [| CreateField.intDV <| this.GetSchemaName schemaName |]
              else None }
    
    override __.UpdateFieldTemplate (template : FieldTemplate) (value : int) = 
        template.Fields.[0].SetIntValue(value)
        if template.DocValues.IsSome then 
            // Numeric doc values can only be saved as Int64 
            template.DocValues.Value.[0].SetLongValue(int64 value)
    
    override this.Validate(value : string) = pInt this.DefaultValue value
    override __.GetRangeQuery = Some <| getRangeQuery

/// Field that indexes double values for efficient range filtering and sorting.
type DoubleField() = 
    inherit FieldBase<double>(DoubleField.TYPE_STORED, SortFieldType.DOUBLE, 0.0, None)
    let getRangeQuery schemaName (lowerRange, upperRange) (inclusiveMinimum, inclusiveMaximum) = 
        maybe 
            { 
            let! lower = parseNumber<double, java.lang.Double> (schemaName, "Double") lowerRange JavaDoubleMin 
                             Double.TryParse javaDouble
            let! upper = parseNumber<double, java.lang.Double> (schemaName, "Double") upperRange JavaDoubleMax 
                             Double.TryParse javaDouble
            return NumericRangeQuery.NewDoubleRange(schemaName, lower, upper, inclusiveMinimum, inclusiveMaximum) :> Query }
    static member Instance = new DoubleField() :> FieldBase
    override this.Validate(value : string) = pDouble this.DefaultValue value
    
    override this.CreateFieldTemplate (schemaName : string) (generateDocValues : bool) = 
        { Fields = [| CreateField.double <| this.GetSchemaName schemaName |]
          DocValues = 
              if generateDocValues then Some <| [| CreateField.doubleDV <| this.GetSchemaName schemaName |]
              else None }
    
    override this.UpdateFieldTemplate (template : FieldTemplate) (value : double) = 
        template.Fields.[0].SetDoubleValue(value)
        if template.DocValues.IsSome then template.DocValues.Value.[0].SetDoubleValue(value)
    
    override __.GetRangeQuery = Some <| getRangeQuery

/// Field that indexes float values for efficient range filtering and sorting.
type FloatField() as self = 
    inherit FieldBase<float32>(FloatField.TYPE_STORED, SortFieldType.FLOAT, float32 0.0, None)
    let getRangeQuery schemaName (lowerRange, upperRange) (inclusiveMinimum, inclusiveMaximum) = 
        maybe 
            { 
            let! lower = parseNumber<float32, java.lang.Float> (schemaName, "Float") lowerRange JavaFloatMin 
                             Single.TryParse javaFloat
            let! upper = parseNumber<float32, java.lang.Float> (schemaName, "Float") upperRange JavaFloatMax 
                             Single.TryParse javaFloat
            return NumericRangeQuery.NewFloatRange(schemaName, lower, upper, inclusiveMinimum, inclusiveMaximum) :> Query }
    static member Instance = new FloatField() :> FieldBase
    override __.Validate(value : string) = pFloat self.DefaultValue value
    
    override __.CreateFieldTemplate (schemaName : string) (generateDocValues : bool) = 
        { Fields = [| CreateField.float <| self.GetSchemaName schemaName |]
          DocValues = 
              if generateDocValues then Some <| [| CreateField.floatDV <| self.GetSchemaName schemaName |]
              else None }
    
    override this.UpdateFieldTemplate (template : FieldTemplate) (value : float32) = 
        template.Fields.[0].SetFloatValue(value)
        if template.DocValues.IsSome then template.DocValues.Value.[0].SetFloatValue(value)
    
    override __.GetRangeQuery = Some <| getRangeQuery

/// Field that indexes long values for efficient range filtering and sorting.
type LongField(defaultValue : int64, ?defaultFieldName) as self = 
    inherit FieldBase<int64>(LongField.TYPE_STORED, SortFieldType.LONG, defaultValue, defaultFieldName)
    let getRangeQuery schemaName (lowerRange, upperRange) (inclusiveMinimum, inclusiveMaximum) = 
        maybe 
            { 
            let! lower = parseNumber<int64, java.lang.Long> (schemaName, "Long") lowerRange JavaLongMin Int64.TryParse 
                             javaLong
            let! upper = parseNumber<int64, java.lang.Long> (schemaName, "Long") upperRange JavaLongMax Int64.TryParse 
                             javaLong
            return NumericRangeQuery.NewLongRange(schemaName, lower, upper, inclusiveMinimum, inclusiveMaximum) :> Query }
    static member Instance = new LongField(0L) :> FieldBase
    override __.Validate(value : string) = pLong self.DefaultValue value
    
    override __.CreateFieldTemplate (schemaName : string) (generateDocValues : bool) = 
        { Fields = [| CreateField.long <| self.GetSchemaName schemaName |]
          DocValues = 
              if generateDocValues then Some <| [| CreateField.longDV <| self.GetSchemaName schemaName |]
              else None }
    
    override this.UpdateFieldTemplate (template : FieldTemplate) (value : int64) = 
        template.Fields.[0].SetLongValue(value)
        if template.DocValues.IsSome then template.DocValues.Value.[0].SetLongValue(value)
    
    override __.GetRangeQuery = Some <| getRangeQuery

/// Field that indexes date time values for efficient range filtering and sorting.
/// It only supports a fixed YYYYMMDDHHMMSS format.
type DateTimeField() as self = 
    inherit LongField(00010101000000L) // Equivalent to 00:00:00.0000000, January 1, 0001, in the Gregorian calendar
    static member Instance = new DateTimeField() :> FieldBase
    override __.Validate(value : string) = 
        // TODO: Implement custom validation for date time
        pLong self.DefaultValue value

/// Field that indexes date time values for efficient range filtering and sorting.
/// It only supports a fixed YYYYMMDD format.
type DateField() as self = 
    inherit LongField(00010101L) // Equivalent to January 1, 0001, in the Gregorian calendar
    static member Instance = new DateField() :> FieldBase
    override __.Validate(value : string) = 
        // TODO: Implement custom validation for date
        pLong self.DefaultValue value

/// A field that is indexed and tokenized, without term vectors. For example this would be used on 
/// a 'body' field, that contains the bulk of a document's text.
/// Note: This field does not support sorting.
type TextField() as self = 
    inherit FieldBase<string>(TextField.TYPE_STORED, SortFieldType.LONG, "null", None)
    static member Instance = new TextField() :> FieldBase
    
    override this.Validate(value : string) = 
        if String.IsNullOrWhiteSpace value then this.DefaultValue
        else value
    
    override __.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
        { Fields = [| CreateField.text <| self.GetSchemaName schemaName |]
          DocValues = None }
    
    override this.UpdateFieldTemplate (template : FieldTemplate) (value : string) = 
        template.Fields.[0].SetStringValue(value)
    override this.GetRangeQuery = None

/// A field that is indexed but not tokenized: the entire String value is indexed as a single token. 
/// For example this might be used for a 'country' field or an 'id' field, or any field that you 
/// intend to use for sorting or access through the field cache.
type ExactTextField(?defaultFieldName) as self = 
    inherit FieldBase<String>(StringField.TYPE_STORED, SortFieldType.SCORE, "null", defaultFieldName)
    let getRangeQuery schemaName (lowerRange : string, upperRange : string) (inclusiveMinimum, inclusiveMaximum) = 
        ok 
        <| (new TermRangeQuery(schemaName, new FlexLucene.Util.BytesRef(Encoding.UTF8.GetBytes lowerRange), 
                               new FlexLucene.Util.BytesRef(Encoding.UTF8.GetBytes upperRange), inclusiveMinimum, 
                               inclusiveMaximum) :> Query)
    static member Instance = new ExactTextField() :> FieldBase
    
    override this.Validate(value : string) = 
        if String.IsNullOrWhiteSpace value then this.DefaultStringValue
        else value.ToLowerInvariant() // ToLower is necessary to make searching case insensitive
    
    override __.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
        { Fields = [| CreateField.text <| self.GetSchemaName schemaName |]
          DocValues = 
              if generateDV then Some <| [| CreateField.stringDV <| self.GetSchemaName schemaName |]
              else None }
    
    override this.UpdateFieldTemplate (template : FieldTemplate) (value : string) = 
        template.Fields.[0].SetStringValue(value)
        if template.DocValues.IsSome then template.DocValues.Value.[0].SetBytesValue(Encoding.UTF8.GetBytes(value))
    
    override __.GetRangeQuery = Some <| getRangeQuery

/// Field that indexes boolean values.
type BoolField() = 
    inherit ExactTextField()
    let trueString = "true"
    let falseString = "false"
    
    let toExternal (value : string) = 
        if String.Equals("t", value, StringComparison.InvariantCultureIgnoreCase) then trueString
        else falseString
    
    static member Instance = new BoolField() :> FieldBase
    
    override this.Validate(value : string) = 
        if String.IsNullOrWhiteSpace value then this.DefaultStringValue
        else if value.StartsWith("t", true, Globalization.CultureInfo.InvariantCulture) then "t"
        else "f"
    
    override __.ToExternal = Some <| toExternal

/// A field which is only stored and is not search-able
type StoredField() as self = 
    inherit FieldBase<string>(StoredField.TYPE, SortFieldType.SCORE, "null", None)
    static member Instance = new StoredField() :> FieldBase
    
    override this.Validate(value : string) = 
        if String.IsNullOrWhiteSpace value then this.DefaultValue
        else value
    
    override __.CreateFieldTemplate (schemaName : string) (generateDV : bool) = 
        { Fields = [| CreateField.text <| self.GetSchemaName schemaName |]
          DocValues = None }
    
    override this.UpdateFieldTemplate (template : FieldTemplate) (value : string) = 
        template.Fields.[0].SetStringValue(value)
    override __.GetRangeQuery = None

/// ----------------------------------------------------------------------
/// Extended field types
/// ----------------------------------------------------------------------
/// Field to be used for time stamp. This field is used by index to capture the modification
/// time.
/// It only supports a fixed YYYYMMDDHHMMSSfff format.
type TimeStampField() = 
    inherit LongField(00010101000000000L, TimeStampField.Name)
    static do addToMetaFields TimeStampField.Name
    static member Name = "_timestamp"
    static member Instance = new TimeStampField() :> FieldBase
    override this.UpdateDocument document schemaName fieldSource template = 
        // The timestamp value will always be auto generated
        this.UpdateFieldTemplate template (GetCurrentTimeAsLong())

/// Used for representing the id of an index
type IdField() = 
    inherit ExactTextField(IdField.Name)
    static do addToMetaFields IdField.Name
    static member Name = "_id"
    static member Instance = new IdField() :> FieldBase
    override this.UpdateDocument document schemaName fieldSource template = 
        this.Validate document.Id |> this.UpdateFieldTemplate template

/// Used for representing the id of an index
type StateField() = 
    inherit ExactTextField(StateField.Name)
    static do addToMetaFields StateField.Name
    static member Name = "_state"
    static member Instance = new StateField() :> FieldBase
    static member Active = "active"
    static member Inactive = "inactive"
    override this.UpdateDocument document schemaName fieldSource template = 
        // Set it to Active for all normal indexing requests
        this.UpdateFieldTemplate template StateField.Active
    /// Helper method to set the state to Inactive
    member this.UpdateFieldToInactive template = this.UpdateFieldTemplate template StateField.Inactive

/// This field is used to add causal ordering to the events in 
/// the index. A document with lower modify index was created/updated before
/// a document with the higher index.
/// This is also used for concurrency updates.
type ModifyIndexField() = 
    inherit LongField(0L, ModifyIndexField.Name)
    static do addToMetaFields ModifyIndexField.Name
    static member Name = "_modifyindex"
    static member Instance = new ModifyIndexField() :> FieldBase
    override this.UpdateDocument document schemaName fieldSource template = 
        this.UpdateFieldTemplate template document.ModifyIndex
