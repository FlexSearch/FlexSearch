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
open FlexSearch.Api.Model
open System.Linq

module DocumentTemplate = 
    type NumericDocValuesField = FlexLucene.Document.NumericDocValuesField
    
    /// This is responsible for creating a wrapper around Document which can be cached and re-used.
    /// Note: Make sure that the template is not accessed by multiple threads.
    type T = 
        { Setting : IndexSetting.T
          TemplateFields : array<FieldTemplate>
          Template : LuceneDocument
          MetaDataFieldCount : int }
    
    let inline protectedFields (fieldName) = fieldName = IdField.Name || fieldName = TimeStampField.Name
    
    /// Create a new document template
    let create (s : IndexSetting.T) = 
        let template = new LuceneDocument()
        let fields = new ResizeArray<FieldTemplate>()
        
        let add (field : FieldTemplate) = 
            template.Add(field.Fields.[0])
            if field.DocValues.IsSome then template.Add(field.DocValues.Value.[0])
            fields.Add(field)
        
        let metaDataFields = FieldSchema.getMetaFieldsTemplates()
        metaDataFields |> Array.iter (fun f -> add (f))
        for field in s.Fields.Skip(metaDataFields.Count()) do
            let hasDocValues = FieldSchema.hasDocValues field
            FieldSchema.hasDocValues field
            |> field.FieldType.CreateTemplate field.SchemaName
            |> add
        { Setting = s
          TemplateFields = fields.ToArray()
          Template = template
          MetaDataFieldCount = metaDataFields.Count() }
    
    /// Update the lucene Document based upon the passed FlexDocument.
    /// Note: Do not update the document from multiple threads.
    let updateTempate (document : Document) (modifyIndex : int64) (template : T) = 
        // Update meta fields
        // Id Field
        template.TemplateFields.[0].Fields.[0].SetStringValue(document.Id)
        // Timestamp fields
        template.TemplateFields.[1].Fields.[0].SetLongValue(document.TimeStamp)
        template.TemplateFields.[1].DocValues.Value.[0].SetLongValue(document.TimeStamp)
        template.TemplateFields.[2].Fields.[0].SetLongValue(modifyIndex)
        template.TemplateFields.[2].DocValues.Value.[0].SetLongValue(modifyIndex)
        // Performance of F# iter is very slow here.
        for i = template.MetaDataFieldCount to template.TemplateFields.Length - 1 do
            let field = template.Setting.Fields.[i]
            
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
                v |> Field.updateLuceneField field template.TemplateFields.[i].LuceneField false
                if field.GenerateDocValue then 
                    v |> Field.updateLuceneField field template.TemplateFields.[i].DocValue.Value true
            | None -> 
                Field.updateLuceneFieldToDefault field false template.TemplateFields.[i].LuceneField
                if field.GenerateDocValue then 
                    Field.updateLuceneFieldToDefault field true template.TemplateFields.[i].DocValue.Value
        template.Template
