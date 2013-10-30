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

open FlexSearch.Api
open FlexSearch.Utility
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
open org.apache.lucene.facet.search
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

open System
open System.ComponentModel.Composition
open System.Collections.Generic
open System.IO
open System.Threading.Tasks.Dataflow
open System.Reflection
open System.Threading


// ----------------------------------------------------------------------------
// Contains all the flex iterface definitions 
// ----------------------------------------------------------------------------
[<AutoOpen>]
module Interface =
    
    // ---------------------------------------------------------------------------- 
    // General Interface to offload all resource loading resposibilities. This will
    // be used to parse settings, load text files etc. This will enable easy mocking 
    // and central management of all such activities
    // ---------------------------------------------------------------------------- 
    type IResourceLoader =
        // Reads the resource from the location and returns all the content as a string
        abstract member LoadResourceAsString    :   string -> string

        // Reads the resource and returns it as a List<string>. Also ignores
        // any blank lines or lines starting with #. Mostly used by filters
        abstract member LoadResourceAsList      :   string -> List<string>

        // Reads the resource and returns it as a List<string[]>. This is used to load 
        // settings files in the below format
        // test:test1,test2
        // Here all the colon & comma separated stuff will be returned as the member of the array.
        // This is used by certain filters to load map kind of data where first field maps to
        // a number of secondary fields. Also ignores
        // any blank lines or lines starting with #. Mostly used by filters
        abstract member LoadResourceAsMap       :   string -> List<string[]>
        

    // ---------------------------------------------------------------------------- 
    // General fatory Interface for all mef based factories
    // ---------------------------------------------------------------------------- 
    type IFlexFactory<'a> = 
        abstract member GetModuleByName     :   string -> 'a option
        abstract member ModuleExists        :   string -> bool
        abstract member GetAllModules       :   unit -> Dictionary<string, 'a>


    // ---------------------------------------------------------------------------- 
    // Interface class from which all tokenizers will derive
    // ---------------------------------------------------------------------------- 
    type IFlexTokenizerFactory = 
        abstract member Initialize  :   Dictionary<string,string> * IResourceLoader -> unit
        abstract member Create      :   Reader -> Tokenizer
    

    // ---------------------------------------------------------------------------- 
    // Interface from which all filters will derive
    // ----------------------------------------------------------------------------     
    type IFlexFilterFactory = 
        abstract member Initialize  :   Dictionary<string,string> * IResourceLoader -> unit
        abstract member Create      :   TokenStream -> TokenStream


    // ----------------------------------------------------------------------------     
    // The meta data interface which is used to read mef based
    // meta data properties 
    // ---------------------------------------------------------------------------- 
    type IFlexMetaData =
        abstract Name   :   string
    

    // ----------------------------------------------------------------------------     
    // Flex Setting builder interface
    // This will take api objects and tranform them into Flex domain objects
    // ----------------------------------------------------------------------------     
    type ISettingsBuilder =
        abstract BuildSetting           :   Index -> FlexIndexSetting


    // ----------------------------------------------------------------------------     
    // Flex Index validator interface
    // This will validate all index settings. This could be easily replaced by 
    // a higher order function but it makes C# to F# interoperability a bit 
    // difficult
    // ----------------------------------------------------------------------------  
    type IIndexValidator =
        abstract Validate               :   Index -> Unit


    // ----------------------------------------------------------------------------     
    // FlexQuery interface
    // ----------------------------------------------------------------------------     
    type IFlexQuery =
        // abstract member QueryName   :   unit -> string[]
        abstract member GetQuery    :   FlexField * SearchCondition -> Option<Query>


    // ----------------------------------------------------------------------------     
    // Search service interface
    // ----------------------------------------------------------------------------     
    type ISearchService =
        abstract member Search          :   FlexIndex * SearchQuery -> SearchResults
        abstract member SearchProfile   :   FlexIndex * SearchProfileQuery -> SearchResults
        

    // ----------------------------------------------------------------------------     
    // Computation operation interface
    // ----------------------------------------------------------------------------     
    type IComputationOperation =
                                        // destination * sources * parameters
        abstract member Initialize  :   string * string[] * Dictionary<string,string> -> bool
        abstract member Compute     :   IReadOnlyDictionary<string, string> -> string 


    // ----------------------------------------------------------------------------     
    // Search condition interface
    // ----------------------------------------------------------------------------     
    type ISearchCondition =
        abstract member GetCondition    :   Dictionary<string,string> -> string 
    
    // ----------------------------------------------------------------------------     
    // Scripting realted interfaces
    // ----------------------------------------------------------------------------   
    // Compile different types of scripts
    type IScriptFactory<'a> =
        abstract member CompileScript   :   ScriptProperties -> 'a
    

    // Profile selection scripts used to dynamically select an search profile
    type IFlexProfileSelectorScript =
        abstract Execute  :   IReadOnlyDictionary<string, string> -> string


    // Scripts used by dynamic or computed fields
    type IComputedFieldScript =
        abstract Execute  :   IReadOnlyDictionary<string, string> -> string


    // Scripts used for custom scoring
    type ICustomScoringScript =
        abstract Execute  :   IReadOnlyDictionary<string, string> * double -> double


    // A helper factory exposing all the script factories
     type IScriptFactoryCollection =
        abstract member ProfileSelectorScriptFactory    :   IScriptFactory<IFlexProfileSelectorScript>
        abstract member ComputedFieldScriptFactory      :   IScriptFactory<IComputedFieldScript>
        abstract member CustomScoringScriptFactory      :   IScriptFactory<ICustomScoringScript>


    // ---------------------------------------------------------------------------- 
    /// Interface which exposes all top level factories
    /// Could have exposed all these through a simple dictionary over IFlexFactory
    /// but then we would have to perform a look up to get each factory instace.
    /// This is fairly easy to manage as all the logic is in IFlexFactory.
    /// Also reduces passing of parameters.
    // ---------------------------------------------------------------------------- 
    type IFactoryCollection =
         abstract member FilterFactory              :   IFlexFactory<IFlexFilterFactory>
         abstract member TokenizerFactory           :   IFlexFactory<IFlexTokenizerFactory>
         abstract member AnalyzerFactory            :   IFlexFactory<Analyzer>
         abstract member SearchQueryFactory         :   IFlexFactory<IFlexQuery>
         abstract member ComputationOpertionFactory :   IFlexFactory<IComputationOperation>
         //abstract member PluginsFactory             :   IFlexFactory<IPlugin>
         abstract member ScriptFactoryCollection    :   IScriptFactoryCollection
         abstract member ResourceLoader             :   IResourceLoader


    // ---------------------------------------------------------------------------- 
    /// General key value based settings store used across Flex to store all settings
    /// Do not use this as a cache store
    // ---------------------------------------------------------------------------- 
    type IKeyValueStore =
        abstract member GetIndexSetting     :   string -> Option<Index>
        abstract member DeleteIndexSetting  :   string -> unit
        abstract member UpdateIndexSetting  :   Index -> unit
        abstract member GetAllIndexSettings :   unit -> List<Index>
        abstract member GetItem<'T>         :   string -> Option<'T>
        abstract member UpdateItem<'T>      :   string -> 'T -> unit
        abstract member DeleteItem          :   string  -> unit


    // ---------------------------------------------------------------------------- 
    /// Interface which exposes all index related operations
    // ---------------------------------------------------------------------------- 
    type IIndexService =

        /// This method is for synchronous index operations. This will return the 
        /// operation status along with a description message.
        abstract member PerformCommandAsync         :  string * IndexCommand * AsyncReplyChannel<bool * string> -> unit

        // This method is for synchronous index operations. This will return the 
        // operation status along with a description message.
        abstract member PerformCommand              :   string * IndexCommand -> bool * string

        // Index queue which is used for async operations. This is useful for
        // bulk indexing tasks where the producer can send more than one record
        // to the buffer queue.
        abstract member CommandQueue                :   unit -> ActionBlock<string * IndexCommand>

        // Default Search operation. The associated search object will encapsulate
        // all possible search variations
        abstract member PerformQuery                :   string * IndexQuery -> SearchResults

        // Default Search operation. The associated search object will encapsulate
        // all possible search variations
        abstract member PerformQueryAsync           :   string * IndexQuery * AsyncReplyChannel<SearchResults> -> unit
        
        abstract member AddIndex                    :   Index -> unit

        abstract member UpdateIndex                 :   Index -> unit

        abstract member OpenIndex                   :   string -> unit

        abstract member CloseIndex                  :   string -> unit

        abstract member DeleteIndex                 :   string -> unit

        abstract member IndexExists                 :   string -> bool

        abstract member IndexStatus                 :   string -> IndexState

        /// Method to close all indexing indexing operation. Usually called before
        /// a server shutdown.
        abstract member ShutDown                    :   unit -> bool


    // ---------------------------------------------------------------------------- 
    // Interface which exposes all global server settings
    // ---------------------------------------------------------------------------- 
    type IServerSettings =

        // Lucene version to be used across the application
        abstract member LuceneVersion       :   unit -> org.apache.lucene.util.Version

        // Http port
        abstract member HttpPort            :   unit -> int
        
        // Request logger settings
        abstract member LoggerProperties    :   unit -> (bool * int * bool)
        
        // Data folder
        abstract member DataFolder          :   unit -> string

        // Data folder
        abstract member PluginFolder        :   unit -> string

        // Data folder
        abstract member ConfFolder          :   unit -> string

        // Plugins to be loaded
        abstract member PluginsToLoad       :   unit -> string[]