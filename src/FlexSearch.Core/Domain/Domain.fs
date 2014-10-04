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
open FlexSearch.Utility
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.IO
open System.Linq
open System.Reflection
open System.Text.RegularExpressions
open System.Threading
open java.io
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.util
open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

/// <summary>
/// Version cache store used across the system. This helps in resolving 
/// conflicts arising out of concurrent threads trying to update a Lucene document.
/// </summary>
type IVersioningCacheStore = 
    abstract AddOrUpdate : id:string * version:int64 * comparison:int64 -> bool
    abstract Delete : id:string * version:Int64 -> bool
    abstract GetValue : id:string -> int64

/// <summary>
/// Version manager used to manage document version across all shards in an index.
/// We will have one Version Manager per index.
/// </summary>
type IVersionManager = 
    abstract VersionCheck : document:FlexDocument * newVersion:int64 -> Choice<int64, OperationMessage>
    abstract VersionCheck : document:FlexDocument * shardNumber:int * newVersion:int64
     -> Choice<int64, OperationMessage>
    abstract AddOrUpdate : id:string * shardNumber:int * version:int64 * comparison:int64 -> bool
    abstract Delete : id:string * shardNumber:int * version:Int64 -> bool

type IShardMapper = 
    abstract MapToShard : id:string -> int

// ----------------------------------------------------------------------------
// Contains all the indexing related data type definitions 
// ----------------------------------------------------------------------------
/// <summary>
/// Represents the Values which can be used in the query string
/// </summary>
type Value = 
    | SingleValue of string
    | ValueList of string list
    
    member this.GetValueAsList() = 
        match this with
        | SingleValue(v) -> [ v ]
        | ValueList(v) -> v
    
    member this.GetValueAsArray() = 
        match this with
        | SingleValue(v) -> 
            if String.IsNullOrWhiteSpace(v) then Choice2Of2(MISSING_FIELD_VALUE |> GenerateOperationMessage)
            else Choice1Of2([| v |])
        | ValueList(v) -> 
            if v.Length = 0 then Choice2Of2(MISSING_FIELD_VALUE |> GenerateOperationMessage)
            else Choice1Of2(v.ToArray())

/// <summary>
/// Acceptable Predicates for a query
/// </summary>
type Predicate = 
    | NotPredicate of Predicate
    | Condition of FieldName : string * Operator : string * Value : Value * Parameters : Map<string, string> option
    | OrPredidate of Lhs : Predicate * Rhs : Predicate
    | AndPredidate of Lhs : Predicate * Rhs : Predicate

/// <summary>
/// Represents details about field storage related option
/// </summary>
type FieldStoreInformation = 
    { IsStored : bool
      /// Short circuit field to help in bypassing enumeration over field type if a field is 
      /// stored only 
      IsStoredOnly : bool
      /// Helper field to get Lucene compatible store option. This is used like a
      /// cached value so that we don't generate it more than once.
      Store : Field.Store }
    static member Create(isStoredOnly : bool, isStored : bool) = 
        match (isStoredOnly, isStored) with
        | true, _ -> 
            { IsStored = true
              Store = Field.Store.YES
              IsStoredOnly = true }
        | false, true -> 
            { IsStored = true
              Store = Field.Store.YES
              IsStoredOnly = false }
        | false, false -> 
            { IsStored = false
              Store = Field.Store.NO
              IsStoredOnly = false }

/// <summary>
/// Other field related information    
/// </summary>
type FieldInformation = 
    { Boost : int
      EnableFacet : bool }

/// <summary>
///  Advance field properties to be used by custom field
/// </summary>
type FieldIndexingInformation = 
    { Index : bool
      Tokenize : bool
      /// This maps to Lucene's term vectors and is only used for flex custom
      /// data type
      FieldTermVector : FieldTermVector
      /// This maps to Lucene's field index options
      FieldIndexOptions : FieldIndexOptions }

/// <summary>
/// Represents the various data types supported by Flex
/// </summary>
type FlexFieldType = 
    | FlexStored
    | FlexCustom of searchAnalyzer : Analyzer * indexAnalyzer : Analyzer * indexingInformation : FieldIndexingInformation
    | FlexHighlight of searchAnalyzer : Analyzer * indexAnalyzer : Analyzer
    | FlexText of searchAnalyzer : Analyzer * indexAnalyzer : Analyzer
    | FlexExactText of analyzer : Analyzer
    | FlexBool of analyzer : Analyzer
    | FlexDate
    | FlexDateTime
    | FlexInt
    | FlexDouble

/// <summary>
/// General Field which represents the basic properties for the field to be indexed
/// </summary>
type FlexField = 
    { FieldName : string
      SchemaName : string
      StoreInformation : FieldStoreInformation
      PostingsFormat : FieldPostingsFormat
      // DocValuesFormat : FieldDocValuesFormat
      Similarity : FieldSimilarity
      FieldType : FlexFieldType
      Source : System.Func<System.Dynamic.DynamicObject, string> option
      /// This is applicable to all fields apart from stored only so making
      /// it an optional field. 
      FieldInformation : FieldInformation option
      /// Helper property to determine if the field needs any analyzer.
      /// This will save matching effort over field type
      RequiresAnalyzer : bool
      /// Default Lucene field for the flex field. This is used when the 
      /// the data submitted for indexing is invalid.
      DefaultField : Field }

// ----------------------------------------------------------------------------
// Search profile related types
// ----------------------------------------------------------------------------
type ScriptsManager = 
    { ComputedFieldScripts : Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>
      ProfileSelectorScripts : Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>
      CustomScoringScripts : Dictionary<string, System.Dynamic.DynamicObject * double -> double> }

/// <summary>
/// General index settings
/// </summary>
type FlexIndexSetting = 
    { IndexName : string
      IndexAnalyzer : PerFieldAnalyzerWrapper
      SearchAnalyzer : PerFieldAnalyzerWrapper
      Fields : FlexField []
      FieldsLookup : Dictionary<string, FlexField>
      SearchProfiles : Dictionary<string, Predicate * SearchQuery>
      ScriptsManager : ScriptsManager
      IndexConfiguration : IndexConfiguration
      BaseFolder : string
      ShardConfiguration : ShardConfiguration }

/// <summary>
/// Shard writer to write data to physical shard
/// </summary>
type FlexShardWriter = 
    { ShardNumber : int
      SearcherManager : SearcherManager
      ReopenThread : ControlledRealTimeReopenThread
      IndexWriter : IndexWriter
      TrackingIndexWriter : TrackingIndexWriter }

type FlexShard = 
    { ShardNumber : int }

/// <summary>
/// Represents a dummy Lucene document. There will be one per index stored in a dictionary
/// </summary>
type ThreadLocalDocument = 
    { Document : Document
      FieldsLookup : Dictionary<string, Field>
      LastGeneration : int }

/// <summary>
/// Represents an index in Flex terms which may consist of a number of
/// valid Lucene indices.
/// </summary>
type FlexIndex = 
    { IndexSetting : FlexIndexSetting
      Shards : FlexShardWriter []
      ThreadLocalStore : ThreadLocal<ThreadLocalDocument>
      VersioningManager : IVersionManager
      Token : CancellationTokenSource }

/// <summary>
/// Case insensitive keyword analyzer 
/// </summary>
[<Name("CaseInsensitiveKeywordAnalyzer")>]
[<Sealed>]
type CaseInsensitiveKeywordAnalyzer() = 
    inherit Analyzer()
    override this.createComponents (fieldName : string, reader : Reader) = 
        let source = new KeywordTokenizer(reader)
        let result = new LowerCaseFilter(source)
        new org.apache.lucene.analysis.Analyzer.TokenStreamComponents(source, result)
