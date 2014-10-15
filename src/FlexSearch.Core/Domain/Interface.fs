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
open FlexSearch.Api.Messages
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
/// Formatter interface for supporting multiple formats in the HTTP engine
/// </summary>
type IFormatter = 
    abstract SupportedHeaders : unit -> string []
    abstract Serialize : body:obj * stream:Stream -> unit
    abstract SerializeToString : body:obj -> string
    abstract DeSerialize<'T> : stream:Stream -> 'T

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
/// Index related operations
/// </summary>
type IIndexService = 
    abstract GetIndex : string -> Choice<Index, OperationMessage>
    abstract UpdateIndex : Index -> Choice<unit, OperationMessage>
    abstract DeleteIndex : string -> Choice<unit, OperationMessage>
    abstract AddIndex : Index -> Choice<CreateResponse, OperationMessage>
    abstract GetAllIndex : unit -> Choice<List<Index>, OperationMessage>
    abstract IndexExists : string -> bool
    abstract GetIndexStatus : string -> Choice<IndexState, OperationMessage>
    abstract OpenIndex : string -> Choice<unit, OperationMessage>
    abstract CloseIndex : string -> Choice<unit, OperationMessage>
    abstract Commit : string -> Choice<unit, OperationMessage>
    abstract Refresh : string -> Choice<unit, OperationMessage>
    abstract GetIndexSearchers : string -> Choice<List<IndexSearcher>, OperationMessage>

/// <summary>
/// Document related operations
/// </summary>
type IDocumentService = 
    abstract GetDocument : indexName:string * id:string -> Choice<FlexSearch.Api.ResultDocument, OperationMessage>
    abstract GetDocuments : indexName:string * count:int -> Choice<SearchResults, OperationMessage>
    abstract AddOrUpdateDocument : document:FlexDocument -> Choice<unit, OperationMessage>
    abstract DeleteDocument : indexName:string * id:string -> Choice<unit, OperationMessage>
    abstract AddDocument : document:FlexDocument -> Choice<CreateResponse, OperationMessage>
    abstract DeleteAllDocuments : indexName:string -> Choice<unit, OperationMessage>

/// <summary>
/// Queuing related operations
/// </summary>
type IQueueService = 
    abstract AddDocumentQueue : document: FlexDocument -> unit
    abstract AddOrUpdateDocumentQueue : document: FlexDocument -> unit

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
    abstract TraceCritical : message:string * ex:Exception -> unit
    abstract TraceCritical : ex:Exception -> unit
    abstract TraceError : error:string * ex:Exception -> unit
    abstract TraceError : error:string -> unit
    abstract TraceError : error:string * ex:OperationMessage -> unit
    abstract TraceInformation : infoMessage:string * messageDetails:string -> unit

/// <summary>
/// Generic job service interface
/// </summary>
type IJobService = 
    abstract GetJob : string -> Choice<Job, OperationMessage>
    abstract DeleteAllJobs : unit -> Choice<unit, OperationMessage>
    abstract UpdateJob : Job -> Choice<unit, OperationMessage>

/// <summary>
/// General Interface to offload all resource loading responsibilities. This will
/// be used to parse settings, load text files etc.
/// </summary> 
type IResourceService = 
    abstract GetResource<'T> : resourceName:string -> Choice<'T, OperationMessage>
    abstract UpdateResource<'T> : resourceName:string * resource:'T -> Choice<unit, OperationMessage>
    abstract DeleteResource<'T> : resourceName:string -> Choice<unit, OperationMessage>

/// <summary>
/// Import handler interface to support bulk indexing
/// </summary>
type IImportHandler = 
    abstract SupportsBulkIndexing : unit -> bool
    abstract SupportsIncrementalIndexing : unit -> bool
    abstract ProcessBulkRequest : string * ImportRequest -> unit
    abstract ProcessIncrementalRequest : string * ImportRequest -> Choice<unit, OperationMessage>

/// <summary>
/// Interface which exposes all top level factories
/// Could have exposed all these through a simple dictionary over IFlexFactory
/// but then we would have to perform a look up to get each factory instance.
/// This is fairly easy to manage as all the logic is in IFlexFactory.
/// Also reduces passing of parameters.
/// </summary>
type IFactoryCollection = 
    abstract FilterFactory : IFlexFactory<IFlexFilterFactory>
    abstract TokenizerFactory : IFlexFactory<IFlexTokenizerFactory>
    abstract AnalyzerFactory : IFlexFactory<Analyzer>
    abstract SearchQueryFactory : IFlexFactory<IFlexQuery>
    abstract ImportHandlerFactory : IFlexFactory<IImportHandler>
