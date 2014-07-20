// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch.Api
open FlexSearch.Core
open System
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.search

// ----------------------------------------------------------------------------
// Contains all functions related to flex field 
// ----------------------------------------------------------------------------
[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FlexField = 
    // Default value to be used for string data type
    let StringDefaultValue = "null"
    // Default value to be used for flex date data type
    let DateDefaultValue = lazy Int64.Parse("00010101")
    // Default value to be used for date time data type
    let DateTimeDefaultValue = lazy Int64.Parse("00010101000000")
    
    // Field info to be used by flex highlight field
    let FlexHighLightFieldType = 
        lazy (let fieldType = new FieldType()
              fieldType.setStored (true)
              fieldType.setTokenized (true)
              fieldType.setIndexed (true)
              fieldType.setIndexOptions (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
              fieldType.freeze()
              fieldType)
    
    /// <summary>
    /// Gets the sort field associated with the field type. This is used for determining sort style
    /// while searching  
    /// </summary>
    /// <param name="flexField"></param>
    let inline SortField(flexField : FlexField) = 
        match flexField.FieldType with
        | FlexCustom(_, _, _) -> failwithf "Sorting is not possible on string or text data type."
        | FlexStored(_) -> failwithf "Sorting is not possible on string or text data type."
        | FlexText(_) -> failwithf "Sorting is not possible on string or text data type."
        | FlexBool(_) -> SortField.Type.STRING
        | FlexExactText(_) -> SortField.Type.STRING
        | FlexDate(_) -> SortField.Type.LONG
        | FlexDateTime(_) -> SortField.Type.LONG
        | FlexInt(_) -> SortField.Type.INT
        | FlexDouble(_) -> SortField.Type.DOUBLE
        | FlexHighlight(_) -> failwithf "Sorting is not possible on string or text data type."
    
    /// <summary>
    /// Gets the default string value associated with the field type.
    /// </summary>
    /// <param name="flexField"></param>
    let inline DefaultValue flexField = 
        match flexField.FieldType with
        | FlexCustom(_, _, _) -> "null"
        | FlexStored(_) -> "null"
        | FlexText(_) -> "null"
        | FlexBool(_) -> "false"
        | FlexExactText(_) -> "null"
        | FlexDate(_) -> "00010101"
        | FlexDateTime(_) -> "00010101000000"
        | FlexInt(_) -> "0"
        | FlexDouble(_) -> "0.0"
        | FlexHighlight(_) -> "null"
    
    /// <summary>
    /// Creates Lucene's field types. This is only used for FlexCustom data type to
    /// support flexible field type
    /// </summary>
    /// <param name="fieldTermVector"></param>
    /// <param name="stored"></param>
    /// <param name="tokenized"></param>
    /// <param name="indexed"></param>
    let GetFieldTemplate(fieldTermVector : FieldTermVector, stored, tokenized, indexed) = 
        let fieldType = new FieldType()
        fieldType.setStored (stored)
        fieldType.setTokenized (tokenized)
        fieldType.setIndexed (indexed)
        match fieldTermVector with
        | FieldTermVector.DoNotStoreTermVector -> fieldType.setIndexOptions (FieldInfo.IndexOptions.DOCS_ONLY)
        | FieldTermVector.StoreTermVector -> fieldType.setIndexOptions (FieldInfo.IndexOptions.DOCS_AND_FREQS)
        | FieldTermVector.StoreTermVectorsWithPositions -> 
            fieldType.setIndexOptions (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
        | FieldTermVector.StoreTermVectorsWithPositionsandOffsets -> 
            fieldType.setIndexOptions (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
        | _ -> failwithf "Invalid Field term vector"
        fieldType
    
    let inline ParseLong success failure value = 
        match Int64.TryParse(value) with
        | (true, value) -> success value
        | _ -> failure
    
    let inline ParseBoolean success failure value = 
        match Boolean.TryParse(value) with
        | (true, _) -> success
        | _ -> failure
    
    let inline ParseInteger success failure value = 
        match Int32.TryParse(value) with
        | (true, value) -> success value
        | _ -> failure
    
    let inline ParseDouble success failure value = 
        match Double.TryParse(value) with
        | (true, value) -> success value
        | _ -> failure
    
    /// <summary>
    /// Creates a new index field using the passed flex field
    /// </summary>
    /// <param name="flexField"></param>
    /// <param name="value"></param>
    let inline CreateLuceneField flexField (value : string) = 
        match flexField.FieldType with
        | FlexCustom(_, _, b) -> 
            new Field(flexField.FieldName, value, 
                      GetFieldTemplate(b.FieldTermVector, flexField.StoreInformation.IsStored, b.Tokenize, b.Index))
        | FlexStored -> new StoredField(flexField.FieldName, value) :> Field
        | FlexText(_) -> new TextField(flexField.FieldName, value, flexField.StoreInformation.Store) :> Field
        | FlexHighlight(_) -> new Field(flexField.FieldName, value, FlexHighLightFieldType.Force())
        | FlexBool(_) -> 
            ParseBoolean (new TextField(flexField.FieldName, "true", flexField.StoreInformation.Store) :> Field) 
                flexField.DefaultField value
        | FlexExactText(_) -> new TextField(flexField.FieldName, value, flexField.StoreInformation.Store) :> Field
        | FlexDate -> 
            ParseLong (fun x -> new LongField(flexField.FieldName, x, flexField.StoreInformation.Store) :> Field) 
                flexField.DefaultField value
        | FlexDateTime -> 
            ParseLong (fun x -> new LongField(flexField.FieldName, x, flexField.StoreInformation.Store) :> Field) 
                flexField.DefaultField value
        | FlexInt -> 
            ParseInteger (fun x -> new IntField(flexField.FieldName, x, flexField.StoreInformation.Store) :> Field) 
                flexField.DefaultField value
        | FlexDouble -> 
            ParseDouble (fun x -> new DoubleField(flexField.FieldName, x, flexField.StoreInformation.Store) :> Field) 
                flexField.DefaultField value
    
    /// <summary>
    /// Set the value of index field using the passed value
    /// </summary>
    /// <param name="flexField"></param>
    /// <param name="lucenceField"></param>
    /// <param name="value"></param>
    let inline UpdateLuceneField flexField (lucenceField : Field) (value : string) = 
        match flexField.FieldType with
        | FlexCustom(_, _, _) -> lucenceField.setStringValue (value)
        | FlexStored -> lucenceField.setStringValue (value)
        | FlexText(_) -> lucenceField.setStringValue (value)
        | FlexHighlight(_) -> lucenceField.setStringValue (value)
        | FlexExactText(_) -> lucenceField.setStringValue (value)
        | FlexBool(_) -> 
            ParseBoolean (lucenceField.setStringValue ("true")) (lucenceField.setStringValue ("false")) value
        | FlexDate -> 
            ParseLong (fun x -> lucenceField.setLongValue (x)) (lucenceField.setLongValue (DateDefaultValue.Force())) 
                value
        | FlexDateTime -> 
            match Int64.TryParse(value) with
            | (true, value) -> lucenceField.setLongValue (value)
            | _ -> lucenceField.setLongValue (DateTimeDefaultValue.Force())
        | FlexInt -> 
            match Int32.TryParse(value) with
            | (true, value) -> lucenceField.setIntValue (value)
            | _ -> lucenceField.setIntValue (0)
        | FlexDouble -> 
            match Double.TryParse(value) with
            | (true, value) -> lucenceField.setDoubleValue (value)
            | _ -> lucenceField.setDoubleValue (0.0)
    
    /// <summary>
    /// Creates a default Lucene index field for the passed flex field.
    /// </summary>
    /// <param name="flexField"></param>
    let inline CreateDefaultLuceneField flexField = 
        match flexField.FieldType with
        | FlexCustom(_, _, b) -> 
            new Field(flexField.FieldName, "null", 
                      GetFieldTemplate(b.FieldTermVector, flexField.StoreInformation.IsStored, b.Tokenize, b.Index))
        | FlexStored -> new StoredField(flexField.FieldName, "null") :> Field
        | FlexText(_) -> new TextField(flexField.FieldName, "null", flexField.StoreInformation.Store) :> Field
        | FlexHighlight(_) -> new Field(flexField.FieldName, "null", FlexHighLightFieldType.Force())
        | FlexExactText(_) -> new TextField(flexField.FieldName, "null", flexField.StoreInformation.Store) :> Field
        | FlexBool(_) -> new TextField(flexField.FieldName, "false", flexField.StoreInformation.Store) :> Field
        | FlexDate -> 
            new LongField(flexField.FieldName, DateDefaultValue.Force(), flexField.StoreInformation.Store) :> Field
        | FlexDateTime -> 
            new LongField(flexField.FieldName, DateTimeDefaultValue.Force(), flexField.StoreInformation.Store) :> Field
        | FlexInt -> new IntField(flexField.FieldName, 0, flexField.StoreInformation.Store) :> Field
        | FlexDouble -> new DoubleField(flexField.FieldName, 0.0, flexField.StoreInformation.Store) :> Field
    
    /// <summary>
    /// Set the value of index field to the default value
    /// </summary>
    /// <param name="flexField"></param>
    /// <param name="luceneField"></param>
    let inline UpdateLuceneFieldToDefault flexField (luceneField : Field) = 
        match flexField.FieldType with
        | FlexCustom(_, _, _) -> luceneField.setStringValue ("null")
        | FlexStored -> luceneField.setStringValue ("null")
        | FlexText(_) -> luceneField.setStringValue ("null")
        | FlexBool(_) -> luceneField.setStringValue ("false")
        | FlexExactText(_) -> luceneField.setStringValue ("null")
        | FlexHighlight(_) -> luceneField.setStringValue ("null")
        | FlexDate -> luceneField.setLongValue (DateDefaultValue.Force())
        | FlexDateTime -> luceneField.setLongValue (DateTimeDefaultValue.Force())
        | FlexInt -> luceneField.setIntValue (0)
        | FlexDouble -> luceneField.setDoubleValue (0.0)
    
    /// <summary>
    /// Creates per field analyzer for an index from the index field data. These analyzers are used for searching and
    /// indexing rather than the individual field analyzer           
    /// </summary>
    /// <param name="fields"></param>
    /// <param name="isIndexAnalyzer"></param>
    let GetPerFieldAnalyzerWrapper(fields : FlexField [], isIndexAnalyzer : bool) = 
        let analyzerMap = new java.util.HashMap()
        analyzerMap.put (Constants.IdField, new CaseInsensitiveKeywordAnalyzer()) |> ignore
        analyzerMap.put (Constants.TypeField, new CaseInsensitiveKeywordAnalyzer()) |> ignore
        analyzerMap.put (Constants.LastModifiedField, new CaseInsensitiveKeywordAnalyzer()) |> ignore
        fields |> Array.iter (fun x -> 
                      if isIndexAnalyzer then 
                          match x.FieldType with
                          | FlexCustom(a, b, c) -> analyzerMap.put (x.FieldName, b) |> ignore
                          | FlexHighlight(a, b) -> analyzerMap.put (x.FieldName, b) |> ignore
                          | FlexText(a, b) -> analyzerMap.put (x.FieldName, b) |> ignore
                          | FlexExactText(a) -> analyzerMap.put (x.FieldName, a) |> ignore
                          | FlexBool(a) -> analyzerMap.put (x.FieldName, a) |> ignore
                          | FlexDate | FlexDateTime | FlexInt | FlexDouble | FlexStored -> ()
                      else 
                          match x.FieldType with
                          | FlexCustom(a, b, c) -> analyzerMap.put (x.FieldName, a) |> ignore
                          | FlexHighlight(a, _) -> analyzerMap.put (x.FieldName, a) |> ignore
                          | FlexText(a, _) -> analyzerMap.put (x.FieldName, a) |> ignore
                          | FlexExactText(a) -> analyzerMap.put (x.FieldName, a) |> ignore
                          | FlexBool(a) -> analyzerMap.put (x.FieldName, a) |> ignore
                          | FlexDate | FlexDateTime | FlexInt | FlexDouble | FlexStored -> ())
        new PerFieldAnalyzerWrapper(new org.apache.lucene.analysis.standard.StandardAnalyzer(Constants.LuceneVersion), 
                                    analyzerMap)
