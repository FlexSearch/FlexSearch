﻿// ----------------------------------------------------------------------------
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

/// This is responsible for creating a wrapper around Document which can be cached and re-used.
/// Note: Make sure that the template is not accessed by multiple threads.
type DocumentTemplate = 
    { Setting : IndexSetting
      TemplateFields : array<FieldTemplate>
      Template : LuceneDocument
      MetaDataFieldCount : int }

[<Compile(ModuleSuffix)>]
module DocumentTemplate = 
    type NumericDocValuesField = FlexLucene.Document.NumericDocValuesField
    
    let inline protectedFields (fieldName) = fieldName = IdField.Name || fieldName = TimeStampField.Name
    
    /// Create a new document template
    let create (s : IndexSetting) = 
        let template = new LuceneDocument()
        let fields = new ResizeArray<FieldTemplate>()
        
        let add (field : FieldTemplate) = 
            template.Add(field.Fields.[0])
            if field.DocValues.IsSome then template.Add(field.DocValues.Value.[0])
            fields.Add(field)
        
        let metaDataFields = FieldSchema.getMetaFieldsTemplates()
        metaDataFields |> Array.iter (fun f -> add (f))
        for field in s.Fields.Skip(metaDataFields.Count()) do
            field.FieldType.CreateFieldTemplate field |> add
        { Setting = s
          TemplateFields = fields.ToArray()
          Template = template
          MetaDataFieldCount = metaDataFields.Count() }
    
    /// Update the lucene Document based upon the passed FlexDocument.
    /// Note: Do not update the document from multiple threads.
    let updateTempate (document : Document) (scripts : Scripts) (template : DocumentTemplate) = 
        try 
            match scripts.PreIndexScript with
            | Some(s) -> s.Invoke(document)
            | _ -> ()
        with e -> Logger.Log("Script execution error.", e, MessageKeyword.Document, MessageLevel.Error)
        for i = 0 to template.Setting.Fields.Count - 1 do
            let f = template.Setting.Fields.[i]
            let tf = template.TemplateFields.[i]
            f.FieldType.UpdateDocument document f.SchemaName tf
        template.Template
