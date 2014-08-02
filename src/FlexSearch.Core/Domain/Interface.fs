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
open FlexSearch.Api.Message
open FlexSearch.Api.Service
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.IO
open System.Reflection
open System.Threading
open System.Threading.Tasks.Dataflow
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

type IServer = 
    abstract Start : unit -> unit
    abstract Stop : unit -> unit

/// <summary>
/// General key value based settings store used across Flex to store all settings
/// Do not use this as a cache store
/// </summary>
type IPersistanceStore = 
    abstract Get<'T when 'T : equality> : key:string -> Choice<'T, OperationMessage>
    abstract GetAll<'T> : unit -> IEnumerable<'T>
    abstract Put<'T> : key:string * value:'T -> Choice<unit, OperationMessage>
    abstract Delete<'T> : key:string -> Choice<unit, OperationMessage>
    abstract DeleteAll<'T> : unit -> Choice<unit, OperationMessage>

/// <summary>
/// General Interface to offload all resource loading responsibilities. This will
/// be used to parse settings, load text files etc. This will enable easy mocking 
/// and central management of all such activities
/// </summary> 
type IResourceLoader = 
    
    /// <summary>
    /// Reads the resource from the location and returns all the content as a string
    /// </summary>
    abstract LoadResourceAsString : fileName:string -> string
    
    /// <summary>
    /// Reads the resource and returns it as a List<string>.
    /// </summary>
    abstract LoadFilterList : resourceName:string -> Choice<FilterList, OperationMessage>
    
    /// <summary>
    /// Reads the resource and returns it as a map<string,List<string>>.
    /// </summary>
    abstract LoadMapList : resourceName:string -> Choice<MapList, OperationMessage>

/// <summary>
/// General factory Interface for all MEF based factories
/// </summary>
type IFlexFactory<'T> = 
    abstract GetModuleByName : string -> Choice<'T, OperationMessage>
    abstract ModuleExists : string -> bool
    abstract GetAllModules : unit -> Dictionary<string, 'T>
    abstract GetMetaData : string -> Choice<IDictionary<string, obj>, OperationMessage>

/// <summary>
/// Interface to be implemented by all tokenizer
/// </summary>
type IFlexTokenizerFactory = 
    abstract Initialize : IDictionary<string, string> -> Choice<unit, OperationMessage>
    abstract Create : Reader -> Tokenizer

/// <summary>
/// Interface to be implemented by all filters
/// </summary>    
type IFlexFilterFactory = 
    abstract Initialize : IDictionary<string, string> -> Choice<unit, OperationMessage>
    abstract Create : TokenStream -> TokenStream

/// <summary>
/// The meta data interface which is used to read MEF based
/// meta data properties 
/// </summary>
type IFlexMetaData = 
    abstract Name : string

/// <summary>
/// Flex Setting builder interface
/// This will take API objects and transform them into Flex domain objects
/// </summary>
type ISettingsBuilder = 
    abstract BuildSetting : Index -> Choice<FlexIndexSetting, OperationMessage>

/// <summary>
/// Flex Index validator interface
/// This will validate all index settings. This could be easily replaced by 
/// a higher order function but it makes C# to F# interoperability a bit 
/// difficult
/// </summary>
type IIndexValidator = 
    abstract Validate : Index -> Choice<unit, OperationMessage>

/// <summary>
/// FlexQuery interface     
/// </summary>
type IFlexQuery = 
    abstract QueryName : unit -> string []
    abstract GetQuery : FlexField * string [] * Map<string, string> option -> Choice<Query, OperationMessage>

/// <summary>
/// FlexParser interface
/// </summary>
type IFlexParser = 
    abstract Parse : string -> Choice<Predicate, OperationMessage>

/// <summary>
/// Search service interface
/// </summary>
type ISearchService = 
    abstract Search : SearchQuery -> Choice<SearchResults, OperationMessage>
    abstract Search : FlexIndex * SearchQuery -> Choice<SearchResults, OperationMessage>

/// <summary>
/// Version cache store used across the system. This helps in resolving 
/// conflicts arising out of concurrent threads trying to update a Lucene document.
/// </summary>
type IVersioningCacheStore = 
    abstract GetVersion : id:string -> Option<int>
    abstract AddVersion : id:string * version:int -> bool
    abstract UpdateVersion : string * int * int -> bool
    abstract DeleteVersion : string -> bool

/// <summary>
/// Index related operations
/// </summary>
type IIndexService = 
    abstract GetIndex : string -> Choice<Index, OperationMessage>
    abstract UpdateIndex : Index -> Choice<unit, OperationMessage>
    abstract DeleteIndex : string -> Choice<unit, OperationMessage>
    abstract AddIndex : Index -> Choice<unit, OperationMessage>
    abstract GetAllIndex : unit -> Choice<List<Index>, OperationMessage>
    abstract IndexExists : string -> bool
    abstract GetIndexStatus : string -> Choice<IndexState, OperationMessage>
    abstract OpenIndex : string -> Choice<unit, OperationMessage>
    abstract CloseIndex : string -> Choice<unit, OperationMessage>
    abstract Commit : string -> Choice<unit, OperationMessage>
    abstract Refresh : string -> Choice<unit, OperationMessage>

/// <summary>
/// Document related operations
/// </summary>
type IDocumentService = 
    abstract GetDocument : indexName:string * id:string -> Choice<Dictionary<string, string>, OperationMessage>
    abstract GetDocuments : indexName:string -> Choice<List<Dictionary<string, string>>, OperationMessage>
    abstract AddOrUpdateDocument : indexName:string * id:string * fields:Dictionary<string, string>
     -> Choice<unit, OperationMessage>
    abstract DeleteDocument : indexName:string * id:string -> Choice<unit, OperationMessage>
    abstract AddDocument : indexName:string * id:string * fields:Dictionary<string, string>
     -> Choice<unit, OperationMessage>
    abstract DeleteAllDocuments : indexName:string -> Choice<unit, OperationMessage>

/// <summary>
/// Queuing related operations
/// </summary>
type IQueueService = 
    abstract AddDocumentQueue : indexName:string * id:string * fields:Dictionary<string, string> -> unit
    abstract AddOrUpdateDocumentQueue : indexName:string * id:string * fields:Dictionary<string, string> -> unit

/// <summary>
/// Generic logger interface
/// </summary>
type ILogService = 
    abstract AddIndex : indexName:string * indexDetails:Index -> unit
    abstract UpdateIndex : indexName:string * indexDetails:Index -> unit
    abstract DeleteIndex : indexName:string -> unit
    abstract CloseIndex : indexName:string -> unit
    abstract OpenIndex : indexName:string -> unit
    abstract IndexValidationFailed : indexName:string * indexDetails:Index * validationObject:OperationMessage -> unit
    abstract ComponentLoaded : name:string * componentType:string -> unit
    abstract StartSession : unit -> unit
    abstract EndSession : unit -> unit
    abstract Shutdown : unit -> unit
    abstract TraceCritical : ex:Exception -> unit
    abstract TraceError : error:string * ex:Exception -> unit
    abstract TraceErrorMessage : error:string -> unit
    abstract TraceOperationMessageError : error:string * ex:OperationMessage -> unit
/// <summary>
/// Search related operations
/// </summary>
//type ISearch = 
//    abstract Search : SearchQuery -> Choice<SearchResults, OperationMessage>
//    abstract SearchWithFlatResults : SearchQuery -> Choice<List<Dictionary<string, string>>, OperationMessage>
//
///// <summary>
///// Job related operations
///// </summary>
//type IJobOperation = 
//    abstract GetJob : string -> Choice<Job, OperationMessage>
//
///// <summary>
///// Node related operations
///// </summary>
//type INodeOperation = 
//    abstract LoadAllIndex : unit -> unit
//    abstract ShutDown : unit -> bool
