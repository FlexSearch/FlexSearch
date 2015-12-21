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

open FlexLucene.Analysis
open FlexLucene.Analysis.Standard
open FlexLucene.Codecs.Bloom
open FlexLucene.Document
open FlexLucene.Index
open FlexLucene.Search
open FlexLucene.Search.Similarities
open FlexSearch.Api.Models
open FlexSearch.Api.Constants
open FlexSearch.Core
open FlexSearch.Core.DictionaryHelpers
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Threading
open java.util
open System.Diagnostics
open System.Threading.Tasks

/// This module contains all the meta data related fields used throughout the system
[<AutoOpen>]
module MetaFields = 
    /// Represents the ID field in an index
    [<Literal>]
    let IdField = "_id"
    
    Validators.metaFields.Add(IdField) |> ignore
    
    let private idFieldInfo = 
        { Index = true
          Tokenize = false
          FieldTermVector = TermVector.DoNotStoreTermVector
          FieldIndexOptions = IndexOptions.DocsOnly }
    
    /// Field to be used by the Id field
    let getIdField (bloomEnabled) = 
        let fieldType = FieldType.Custom(CaseInsensitiveKeywordAnalyzer, CaseInsensitiveKeywordAnalyzer, idFieldInfo)
        Field.create (IdField, fieldType, false)
    
    type String with
        /// Get a term for the IdField
        member this.IdTerm() = new Term(IdField, this)
    
    /// Represents the date of last modification of a particular 
    /// document
    [<Literal>]
    let LastModifiedField = "_lastmodified"
    
    Validators.metaFields.Add(LastModifiedField) |> ignore
    
    /// Field to be used by time stamp
    let getTimeStampField() = Field.create (LastModifiedField, FieldType.DateTime, true)
    
    /// This field is used to add causal ordering to the events in 
    /// the index. A document with lower modify index was created/updated before
    /// a document with the higher index.
    /// This is also used for concurrency updates.
    [<Literal>]
    let ModifyIndex = "_modifyindex"
    
    Validators.metaFields.Add(ModifyIndex) |> ignore
    
    /// Field to be used to store modify index
    let getModifyIndexField() = Field.create (ModifyIndex, FieldType.Long, true)
    
    /// Represents the state of a document in the index. A document is never truly
    /// deleted from the index and it is kept around with a status of Deleted. This
    /// is done to simplify the replication.
    [<Literal>]
    let State = "_state"
    
    Validators.metaFields.Add(State) |> ignore
    
    /// Field to be used by the Id field
    let getStateField() = 
        let fieldType = FieldType.Custom(CaseInsensitiveKeywordAnalyzer, CaseInsensitiveKeywordAnalyzer, idFieldInfo)
        Field.create (State, fieldType, false)
    
    /// Field which contains the actual content of a document
    [<Literal>]
    let Source = "_source"
    
    Validators.metaFields.Add(Source) |> ignore
    
    /// Field to be used for storing binary document content
    let getSourceField() = 
        let fieldType = FieldType.Stored
        Field.create (Source, fieldType, false)
    
    /// Represents the score of the search result document
    [<Literal>]
    let Score = "_score"
    
    Validators.metaFields.Add(Score) |> ignore
    
    type TemplateField = 
        { LuceneField : LuceneField
          DocValue : LuceneField option }
    
    let getMetaFields (useBloomFilter : bool) = 
        [| getIdField (useBloomFilter)
           getTimeStampField()
           getModifyIndexField()
           getStateField()
           getSourceField() |]
    
    let getLuceneMetaFields() = 
        [| { LuceneField = Field.getTextField (IdField, "", Field.store)
             DocValue = None }
           { LuceneField = Field.getLongField (LastModifiedField, 0L, Field.store)
             DocValue = Some(new NumericDocValuesField(LastModifiedField, 0L) :> LuceneField) }
           { LuceneField = Field.getLongField (ModifyIndex, 0L, Field.store)
             DocValue = Some(new NumericDocValuesField(ModifyIndex, 0L) :> LuceneField) }
           { LuceneField = Field.getStringField (State, "", Field.store)
             DocValue = None }
           { LuceneField = Field.getBinaryField Source
             DocValue = None } |]

type AnalyzerWrapper(?defaultAnalyzer0 : LuceneAnalyzer) = 
    inherit DelegatingAnalyzerWrapper(Analyzer.PER_FIELD_REUSE_STRATEGY)
    let mutable map = conDict<LuceneAnalyzer>()
    let defaultAnalyzer = defaultArg defaultAnalyzer0 (new StandardAnalyzer() :> LuceneAnalyzer)
    
    /// Creates per field analyzer for an index from the index field data. These analyzers are used for searching and
    /// indexing rather than the individual field analyzer
    member __.BuildAnalyzer(fields : Field.FieldCollection, isIndexAnalyzer : bool) = 
        let analyzerMap = conDict<LuceneAnalyzer>()
        analyzerMap.[MetaFields.IdField] <- CaseInsensitiveKeywordAnalyzer
        analyzerMap.[MetaFields.LastModifiedField] <- CaseInsensitiveKeywordAnalyzer
        fields 
        |> Seq.iter 
               (fun x -> 
               if isIndexAnalyzer then 
                   match x.FieldType with
                   | FieldType.T.Custom(a, b, c) -> analyzerMap |> add (x.SchemaName, b)
                   | FieldType.T.Highlight(a, b) -> analyzerMap |> add (x.SchemaName, b)
                   | FieldType.T.Text(a, b) -> analyzerMap |> add (x.SchemaName, b)
                   | FieldType.T.ExactText(a) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.T.Bool(a) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.T.Date | FieldType.T.DateTime | FieldType.T.Int | FieldType.T.Double | FieldType.T.Stored | FieldType.T.Long -> 
                       ()
               else 
                   match x.FieldType with
                   | FieldType.T.Custom(a, b, c) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.T.Highlight(a, _) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.T.Text(a, _) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.T.ExactText(a) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.T.Bool(a) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.T.Date | FieldType.T.DateTime | FieldType.T.Int | FieldType.T.Double | FieldType.T.Stored | FieldType.T.Long -> 
                       ())
        map <- analyzerMap
    
    override this.getWrappedAnalyzer (fieldName) = 
        match map.TryGetValue(fieldName) with
        | true, analyzer -> analyzer
        | _ -> defaultAnalyzer

module Codec = 
    open FlexLucene.Codecs.Lucene53
    open FlexLucene.Codecs.Lucene50
    open FlexLucene.Codecs.Lucene410
    open FlexLucene.Codecs.Lucene41
    open FlexLucene.Codecs
    
    /// Get the default codec associated with the index version
    let getCodec (enableBloomFilter : bool) (version : IndexVersion) = 
        let getPostingsFormat (fieldName : string, enableBloomFilter, defaultFormat) = 
            if fieldName.Equals(MetaFields.IdField) && enableBloomFilter then 
                new BloomFilteringPostingsFormat(defaultFormat) :> PostingsFormat
            else defaultFormat
        match version with
        | IndexVersion.Lucene_5_0_0 -> 
            let postingsFormat = new Lucene50PostingsFormat()
            { new Lucene53Codec() with
                  member this.getPostingsFormatForField (fieldName) = 
                      getPostingsFormat (fieldName, enableBloomFilter, postingsFormat) } :> Codec
            |> ok
        | IndexVersion.Lucene_4_x_x -> 
            let postingsFormat = new Lucene41PostingsFormat()
            { new Lucene410Codec() with
                  member this.getPostingsFormatForField (fieldName) = 
                      getPostingsFormat (fieldName, enableBloomFilter, postingsFormat) } :> Codec
            |> ok
        | unknown -> fail (UnSupportedIndexVersion(unknown.ToString()))

module IndexSetting = 
    /// General index settings
    type T = 
        { IndexName : string
          IndexAnalyzer : AnalyzerWrapper
          SearchAnalyzer : AnalyzerWrapper
          /// Contains all the fields used in the index
          Fields : Field.FieldCollection
          SearchProfiles : IReadOnlyDictionary<string, Predicate * SearchQuery>
          IndexConfiguration : IndexConfiguration
          BaseFolder : string
          ShardConfiguration : ShardConfiguration }

/// Wrapper around SearcherManager to expose .net IDisposable functionality
type RealTimeSearcher(searchManger : SearcherManager) = 
    let indexSearcher = searchManger.Acquire() :?> IndexSearcher
    
    /// Dispose method which will be called automatically through Fody inter-leaving 
    member __.DisposeManaged() = searchManger.Release(indexSearcher)
    
    member __.IndexSearcher = indexSearcher
    
    /// IndexReader provides an interface for accessing a point-in-time view of 
    /// an index. Any changes made to the index via IndexWriter 
    /// will not be visible until a new IndexReader is opened. 
    member __.IndexReader = indexSearcher.GetIndexReader()
    
    interface IDisposable with
        member __.Dispose() : unit = ()
