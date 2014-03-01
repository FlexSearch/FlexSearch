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

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

open FlexSearch.Utility
open FlexSearch.Api
open FlexSearch.Core

open java.io
open java.util

open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.util
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

open System
open System.ComponentModel.Composition
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open System.Threading

// ----------------------------------------------------------------------------
// Contains all functions related to flexfield 
// ----------------------------------------------------------------------------
[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FlexFieldType =
    
    let inline fold storeFunc customFunc highlightFunc textFunc keywordFunc boolFunc dateFunc datetimeFunc intFunc doubleFunc value =
        match value with
        | FlexStored            ->  storeFunc
        | FlexCustom(a,b)       ->  customFunc(a,b)
        | FlexHighlight(a)      ->  highlightFunc(a)
        | FlexText(a)           ->  textFunc(a)
        | FlexExactText(a)      ->  keywordFunc(a)
        | FlexBool(a)           ->  boolFunc(a)
        | FlexDate              ->  dateFunc
        | FlexDateTime          ->  datetimeFunc
        | FlexInt               ->  intFunc      
        | FlexDouble            ->  doubleFunc    
    
           
// ----------------------------------------------------------------------------
// Contains all functions related to flexfield 
// ----------------------------------------------------------------------------
[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FlexField =

    // Default value to be used for string data type
    let StringDefaultValue = "null"

    // Default value to be used for flex date data type
    let DateDefaultValue = lazy Int64.Parse("00010101")

    // Default value to be used for datetime data type
    let DateTimeDefaultValue = lazy Int64.Parse("00010101000000")

    // Field info to be used by flex highlight field
    let FlexHighLightFieldType =
        lazy
        (
            let fieldType = new FieldType()
            fieldType.setStored(true)
            fieldType.setTokenized(true)
            fieldType.setIndexed(true)
            fieldType.setIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
            fieldType.freeze()
            fieldType
        )


    // ----------------------------------------------------------------------------
    // Gets the sort field associated with the field type. This is used for determining sort style
    // while searching 
    // ----------------------------------------------------------------------------
    let inline SortField (flexField: FlexField) =
        match flexField.FieldType with
        | FlexCustom(_,_)       -> failwithf "Sorting is not possible on string or text data type." 
        | FlexStored(_)         -> failwithf "Sorting is not possible on string or text data type." 
        | FlexText(_)           -> failwithf "Sorting is not possible on string or text data type." 
        | FlexBool(_)           -> SortField.Type.STRING
        | FlexExactText(_)      -> SortField.Type.STRING
        | FlexDate(_)           -> SortField.Type.LONG
        | FlexDateTime(_)       -> SortField.Type.LONG
        | FlexInt(_)            -> SortField.Type.INT
        | FlexDouble(_)         -> SortField.Type.DOUBLE
        | FlexHighlight(_)      -> failwithf "Sorting is not possible on string or text data type."  


    // ----------------------------------------------------------------------------
    // Gets the default string value associated with the field type.
    // ----------------------------------------------------------------------------
    let inline DefaultValue flexField =       
        match flexField.FieldType with
        | FlexCustom(_,_)       -> "null"
        | FlexStored(_)         -> "null"
        | FlexText(_)           -> "null"
        | FlexBool(_)           -> "false"
        | FlexExactText(_)      -> "null"
        | FlexDate(_)           -> "00010101"
        | FlexDateTime(_)       -> "00010101000000"
        | FlexInt(_)            -> "0"
        | FlexDouble(_)         -> "0.0"
        | FlexHighlight(_)      -> "null"


    // ----------------------------------------------------------------------------
    // Creates lucene's field types. This is only used for FlexCustom datatype to
    // support flexible field type
    // ----------------------------------------------------------------------------
    let GetFieldTemplate(fieldTermVector: FieldTermVector, stored, tokenized, indexed) =
        let fieldType = new FieldType()
        fieldType.setStored(stored)
        fieldType.setTokenized(tokenized)
        fieldType.setIndexed(indexed)
        
        match fieldTermVector with
        | FieldTermVector.DoNotStoreTermVector                      -> fieldType.setIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY)
        | FieldTermVector.StoreTermVector                           -> fieldType.setIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS)
        | FieldTermVector.StoreTermVectorsWithPositions             -> fieldType.setIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
        | FieldTermVector.StoreTermVectorsWithPositionsandOffsets   -> fieldType.setIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
        | _ -> failwithf "Invalid Field term vector"
        fieldType

    let inline ParseLong success failure value =
        match Int64.TryParse(value) with
        | (true, value) -> success value
        | _             -> failure

    let inline ParseBoolean success failure value =
        match Boolean.TryParse(value) with
        | (true, _)     -> success 
        | _             -> failure

    let inline ParseInteger success failure value =
        match Int32.TryParse(value) with
        | (true, value) -> success value
        | _             -> failure

    let inline ParseDouble success failure value =
        match Double.TryParse(value) with
        | (true, value) -> success value
        | _             -> failure
 

    // ----------------------------------------------------------------------------
    // Creates a new index field using the passed flex field
    // ----------------------------------------------------------------------------
    let inline CreateLuceneField flexField (value:string) =
        match flexField.FieldType with
        | FlexCustom(_,b) ->
            new Field(flexField.FieldName, value, GetFieldTemplate(b.FieldTermVector, flexField.StoreInformation.IsStored, b.Tokenize, b.Index))  
        
        | FlexStored ->
            new StoredField(flexField.FieldName, value) :> Field
        
        | FlexText(_) ->
            new TextField(flexField.FieldName, value, flexField.StoreInformation.Store) :> Field
        
        | FlexHighlight(_) -> 
            new Field(flexField.FieldName, value, FlexHighLightFieldType.Force())    
        
        | FlexBool(_) -> 
            ParseBoolean (new TextField(flexField.FieldName, "true", flexField.StoreInformation.Store) :> Field) flexField.DefaultField value
        
        | FlexExactText(_) -> 
            new TextField (flexField.FieldName, value, flexField.StoreInformation.Store) :> Field
        
        | FlexDate ->   
            ParseLong (fun x -> new LongField(flexField.FieldName, x, flexField.StoreInformation.Store) :> Field) flexField.DefaultField value
        
        | FlexDateTime ->
            ParseLong (fun x -> new LongField(flexField.FieldName, x, flexField.StoreInformation.Store) :> Field) flexField.DefaultField value
        
        | FlexInt ->
            ParseInteger (fun x -> new IntField(flexField.FieldName, x, flexField.StoreInformation.Store) :> Field) flexField.DefaultField value
        
        | FlexDouble ->
            ParseDouble (fun x -> new DoubleField(flexField.FieldName, x, flexField.StoreInformation.Store) :> Field) flexField.DefaultField value

    
    // ----------------------------------------------------------------------------
    // Set the value ofindex field using the passed value
    // ----------------------------------------------------------------------------
    let inline UpdateLuceneField flexField (lucenceField: Field) (value:string) =
       match flexField.FieldType with
        | FlexCustom(_,_)       -> lucenceField.setStringValue(value)    
        | FlexStored            -> lucenceField.setStringValue(value)    
        | FlexText(_)           -> lucenceField.setStringValue(value)    
        | FlexHighlight(_)      -> lucenceField.setStringValue(value)  
        | FlexExactText(_)      -> lucenceField.setStringValue(value)   
         
        | FlexBool(_)   -> ParseBoolean (lucenceField.setStringValue("true")) (lucenceField.setStringValue("false")) value
        | FlexDate -> ParseLong (fun x -> lucenceField.setLongValue(x)) (lucenceField.setLongValue(DateDefaultValue.Force())) value
        
        | FlexDateTime ->  
            match Int64.TryParse(value) with
            | (true, value) -> lucenceField.setLongValue(value)    
            | _             -> lucenceField.setLongValue(DateTimeDefaultValue.Force()) 
        
        | FlexInt ->
            match Int32.TryParse(value) with
            | (true, value) -> lucenceField.setIntValue(value)
            | _             -> lucenceField.setIntValue(0)
        
        | FlexDouble -> 
            match Double.TryParse(value) with
            | (true, value) -> lucenceField.setDoubleValue(value)
            | _             -> lucenceField.setDoubleValue(0.0)  


    // ----------------------------------------------------------------------------
    // Creates a default lucene index field for the passed flex field.
    // ----------------------------------------------------------------------------
    let inline CreateDefaultLuceneField flexField =
        match flexField.FieldType with
        | FlexCustom(_,b)   -> new Field(flexField.FieldName, "null", GetFieldTemplate(b.FieldTermVector, flexField.StoreInformation.IsStored, b.Tokenize, b.Index))  
        | FlexStored        -> new StoredField(flexField.FieldName, "null") :> Field
        | FlexText(_)       -> new TextField(flexField.FieldName, "null", flexField.StoreInformation.Store) :> Field
        | FlexHighlight(_)  -> new Field(flexField.FieldName, "null", FlexHighLightFieldType.Force())
        | FlexExactText(_)  -> new TextField(flexField.FieldName, "null", flexField.StoreInformation.Store) :> Field
        | FlexBool(_)       -> new TextField(flexField.FieldName, "false", flexField.StoreInformation.Store) :> Field 
        | FlexDate          -> new LongField(flexField.FieldName, DateDefaultValue.Force(), flexField.StoreInformation.Store) :> Field   
        | FlexDateTime      -> new LongField(flexField.FieldName, DateTimeDefaultValue.Force(), flexField.StoreInformation.Store) :> Field
        | FlexInt           -> new IntField(flexField.FieldName, 0, flexField.StoreInformation.Store) :> Field
        | FlexDouble        -> new DoubleField(flexField.FieldName, 0.0, flexField.StoreInformation.Store) :> Field


    // ----------------------------------------------------------------------------
    // Set the value of index field to the default value
    // ----------------------------------------------------------------------------
    let inline UpdateLuceneFieldToDefault flexField (luceneField: Field) =
       match flexField.FieldType with
        | FlexCustom(_,_)       -> luceneField.setStringValue("null")    
        | FlexStored            -> luceneField.setStringValue("null")    
        | FlexText(_)           -> luceneField.setStringValue("null")    
        | FlexBool(_)           -> luceneField.setStringValue("false")    
        | FlexExactText(_)      -> luceneField.setStringValue("null") 
        | FlexHighlight(_)      -> luceneField.setStringValue("null")   
        | FlexDate              -> luceneField.setLongValue(DateDefaultValue.Force())
        | FlexDateTime          -> luceneField.setLongValue(DateTimeDefaultValue.Force()) 
        | FlexInt               -> luceneField.setIntValue(0)
        | FlexDouble            -> luceneField.setDoubleValue(0.0)  


    // ----------------------------------------------------------------------------
    // Creates per field analyzer for an index from the index field data. These analyzers are used for searching and
    // indexing rather than the individual field analyzer           
    // ----------------------------------------------------------------------------
    let GetPerFieldAnalyzerWrapper(fields: FlexField[], isIndexAnalyzer: bool) = 
        let analyzerMap = new java.util.HashMap() 
        analyzerMap.put("id", new CaseInsensitiveKeywordAnalyzer()) |> ignore
        analyzerMap.put("type", new CaseInsensitiveKeywordAnalyzer()) |> ignore
        analyzerMap.put("lastmodified", new CaseInsensitiveKeywordAnalyzer()) |> ignore

        fields |> Array.iter(
            fun x ->
                if isIndexAnalyzer then
                    match x.FieldType with
                    | FlexCustom(a,b)       ->  analyzerMap.put(x.FieldName, a.IndexAnalyzer) |> ignore
                    | FlexHighlight(a)      ->  analyzerMap.put(x.FieldName, a.IndexAnalyzer) |> ignore
                    | FlexText(a)           ->  analyzerMap.put(x.FieldName, a.IndexAnalyzer) |> ignore
                    | FlexExactText(a)      ->  analyzerMap.put(x.FieldName, a) |> ignore
                    | FlexBool(a)           ->  analyzerMap.put(x.FieldName, a) |> ignore
                    | FlexDate             
                    | FlexDateTime         
                    | FlexInt                    
                    | FlexDouble               
                    | FlexStored            ->  ()
                else
                    match x.FieldType with
                    | FlexCustom(a,b)       ->  analyzerMap.put(x.FieldName, a.SearchAnalyzer) |> ignore
                    | FlexHighlight(a)      ->  analyzerMap.put(x.FieldName, a.SearchAnalyzer) |> ignore
                    | FlexText(a)           ->  analyzerMap.put(x.FieldName, a.SearchAnalyzer) |> ignore
                    | FlexExactText(a)      ->  analyzerMap.put(x.FieldName, a) |> ignore
                    | FlexBool(a)           ->  analyzerMap.put(x.FieldName, a) |> ignore
                    | FlexDate             
                    | FlexDateTime         
                    | FlexInt                    
                    | FlexDouble               
                    | FlexStored            ->  ()
        )

        new PerFieldAnalyzerWrapper(new org.apache.lucene.analysis.standard.StandardAnalyzer(Constants.LuceneVersion), analyzerMap)     