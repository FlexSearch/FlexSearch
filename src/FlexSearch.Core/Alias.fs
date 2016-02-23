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

open FlexSearch.Api.Model
open System
open System.Collections.Generic

// type mappings to avoid name conflict
type LuceneAnalyzer = FlexLucene.Analysis.Analyzer

type LuceneDocument = FlexLucene.Document.Document

type LuceneField = FlexLucene.Document.Field

type LuceneFieldType = FlexLucene.Document.FieldType

type LuceneSimilarity = FlexLucene.Search.Similarities.Similarity

type LuceneTextField = FlexLucene.Document.TextField

type FlexDocument = FlexSearch.Api.Model.Document

type FieldSource = Func<string, string, IReadOnlyDictionary<string, string>, string [], string> * string []

type FieldName = string

type Fields = Dictionary<string, string>

type SchemaName = string

type FieldValue = string

type LowerRange = string

type UpperRange = string

type InclusiveMinimum = bool

type InclusiveMaximum = bool

type Token = string

type GetAnalyzer = string -> Result<LuceneAnalyzer>

type ComputedDelegate = Func<string, string, IReadOnlyDictionary<string, string>, string [], string>

type PostSearchDeletegate = Func<SearchQuery, string, float32, Dictionary<string, string>, bool * float32>

type PreSearchDelegate = Action<SearchQuery>

type GetScript = string -> Result<ComputedDelegate * string []>

type Compile = CompilationRepresentationAttribute

type JInt = java.lang.Integer

type JLong = java.lang.Long

type JDouble = java.lang.Double

type JFloat = java.lang.Float

[<AutoOpen>]
module Alias = 
    [<Literal>]
    let ModuleSuffix = CompilationRepresentationFlags.ModuleSuffix
