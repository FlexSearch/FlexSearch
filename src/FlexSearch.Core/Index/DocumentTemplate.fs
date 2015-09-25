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

open FlexSearch.Core

module DocumentTemplate = 
    type NumericDocValuesField = FlexLucene.Document.NumericDocValuesField
    
    /// This is responsible for creating a wrapper around Document which can be cached and re-used.
    /// Note: Make sure that the template is not accessed by multiple threads.
    type T = 
        { Setting : IndexSetting.T
          TemplateFields : array<LuceneField>
          Template : LuceneDocument }
    
    let inline protectedFields (fieldName) = fieldName = Constants.IdField || fieldName = Constants.LastModifiedField
    
    /// Create a new document template            
    let create (s : IndexSetting.T) = 
        let template = new LuceneDocument()
        let fields = new ResizeArray<LuceneField>()
        
        let add (field) = 
            template.Add(field)
            fields.Add(field)
        add (Field.getTextField (s.FieldsLookup.[Constants.IdField].SchemaName, "", Field.store))
        add (Field.getLongField (s.FieldsLookup.[Constants.LastModifiedField].SchemaName, int64 0, Field.store))
        add (new NumericDocValuesField(s.FieldsLookup.[Constants.LastModifiedField].SchemaName, int64 0))
        for field in s.Fields do
            // Ignore these 4 fields here.
            if not (protectedFields (field.FieldName)) then 
                add (Field.createDefaultLuceneField (field))
                if field.GenerateDocValue then 
                    match Field.createDocValueField (field) with
                    | Some(docField) -> add (docField)
                    | _ -> ()
        { Setting = s
          TemplateFields = fields.ToArray()
          Template = template }
    
    /// Update the lucene Document based upon the passed FlexDocument.
    /// Note: Do not update the document from multiple threads.
    let updateTempate (document : Document) (template : T) = 
        // Update meta fields
        // Id Field
        template.TemplateFields.[0].SetStringValue(document.Id)
        // Timestamp fields
        template.TemplateFields.[1].SetLongValue(document.TimeStamp)
        template.TemplateFields.[2].SetLongValue(document.TimeStamp)
        // Performance of F# iter is very slow here.
        let mutable i = 2
        for field in template.Setting.Fields do
            i <- i + 1
            // Ignore these 3 fields here.
            if not (protectedFields (field.FieldName)) then 
                let value = 
                    // If it is computed field then generate and add it otherwise follow standard path
                    match field.Source with
                    | Some(s, options) -> 
                        try 
                            // Wrong values for the data type will still be handled as update Lucene field will
                            // check the data type
                            let value = s.Invoke(document.IndexName, field.FieldName, document.Fields, options)
                            Some <| value
                        with _ -> None
                    | None -> 
                        match document.Fields.TryGetValue(field.FieldName) with
                        | (true, value) -> Some <| value
                        | _ -> None
                match value with
                | Some(v) -> 
                    v |> Field.updateLuceneField field template.TemplateFields.[i] false
                    if field.GenerateDocValue then 
                        v |> Field.updateLuceneField field template.TemplateFields.[i + 1] true
                        i <- i + 1
                | None -> 
                    Field.updateLuceneFieldToDefault field false template.TemplateFields.[i]
                    if field.GenerateDocValue then 
                        Field.updateLuceneFieldToDefault field true template.TemplateFields.[i + 1]
                        i <- i + 1
        template.Template
