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
open System.Reflection
open System.Text.RegularExpressions
open System.Threading
open System.Linq       

// ----------------------------------------------------------------------------
// Contains all the indexing related datatype definitions 
// ----------------------------------------------------------------------------

// Represents details about field storage related option
type FieldStoreInformation = 
    {
        IsStored        :   bool

        // Short circuit field to help in bypassing enumeration over field type if a field is 
        // stored only 
        IsStoredOnly    :   bool

        // Helper field to get luncene compatible store option. This is used like a
        // cached value so that we don't generate it more than once.
        Store       :   Field.Store
    }
    static member Create(isStoredOnly: bool, isStored: bool) =
        match (isStoredOnly, isStored) with
        | true, _       ->  {IsStored = true; Store = Field.Store.YES; IsStoredOnly = true}
        | false, true   ->  {IsStored = true; Store = Field.Store.YES; IsStoredOnly = false}
        | false, false  ->  {IsStored = false; Store = Field.Store.NO; IsStoredOnly = false}
        

// Represents the various analyzers associated with
// a field
type FieldAnalyzers =
    {
        SearchAnalyzer  :   Analyzer
        IndexAnalyzer   :   Analyzer
    }
    

// Other field related information    
type FieldInformation =
    {
        Boost               :   int
        EnableFacet         :   bool
    }


// Advance field properties to be used by custom field
type FieldIndexingInformation =
    {
        Index               :   bool
        Tokenize            :   bool

        // This maps to lucene's term vectors and is only used for flex custom
        // data type
        FieldTermVector     :   FieldTermVector        
    }


// ----------------------------------------------------------------------------
// Represents the various data types supported by Flex
// ----------------------------------------------------------------------------
type FlexFieldType =
    | FlexStored
    | FlexCustom        of FieldAnalyzers * FieldIndexingInformation
    | FlexHighlight     of FieldAnalyzers 
    | FlexText          of FieldAnalyzers     
    | FlexExactText     of Analyzer      
    | FlexBool          of Analyzer      
    | FlexDate                
    | FlexDateTime            
    | FlexInt                 
    | FlexDouble              


// ----------------------------------------------------------------------------
// General Field which represents the basic properties for the field to be
// indexed 
// ----------------------------------------------------------------------------
type FlexField =
    {
        FieldName               :   string
        StoreInformation        :   FieldStoreInformation
        FieldType               :   FlexFieldType

        Source                  :   (IReadOnlyDictionary<string, string> -> string) option

        // This is applicable to all fields apart from stored only so making
        // it an optional field. 
        FieldInformation        :   FieldInformation option

        // Helper property to determine if the field needs any analyzer.
        // This will save matching effort over field type
        RequiresAnalyzer        :   bool
        
        // Default lucene field for the flex field. This is used when the 
        // the data submitted for indexing is invalid.
        DefaultField            :   Field  
    }


// All the valid states possible for an index
type IndexState =
    | Opening
    | Online
    | Offline
    | Closing


// ----------------------------------------------------------------------------
// Search profile related types
// ----------------------------------------------------------------------------
type ScriptsManager = 
    {
        ProfileSelectorScripts      :   Dictionary<string, (IReadOnlyDictionary<string, string> -> string)>
        CustomScoringScripts        :   Dictionary<string, (IReadOnlyDictionary<string, string> * double -> double)>
    }

// General index settings
type FlexIndexSetting =
    {
        IndexName               :   string
        IndexAnalyzer           :   PerFieldAnalyzerWrapper
        SearchAnalyzer          :   PerFieldAnalyzerWrapper
        Fields                  :   FlexField[]
        FieldsLookup            :   Dictionary<string, FlexField>
        SearchProfiles          :   Dictionary<string, SearchQuery>
        ScriptsManager          :   ScriptsManager   
        IndexConfig             :   IndexConfiguration
        BaseFolder              :   string
    }
    

// Shard writer to write data to physical shard
type FlexShardWriter =
    {
        ShardNumber             :   int
        NRTManager              :   SearcherManager
        ReopenThread            :   ControlledRealTimeReopenThread
        IndexWriter             :   IndexWriter
        TrackingIndexWriter     :   TrackingIndexWriter
    }
    

/// Represents an index in Flex terms which may consist of a number of
/// valid lucene indices.
type FlexIndex =
    {
        IndexSetting        :   FlexIndexSetting
        Shards              :   FlexShardWriter[]
//      State               :   IndexState
        Token               :   CancellationTokenSource
    }


// ----------------------------------------------------------------------------
/// Case insensitive keyword analyzer 
// ----------------------------------------------------------------------------
[<Export(typeof<Analyzer>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "CaseInsensitiveKeywordAnalyzer")>]
type CaseInsensitiveKeywordAnalyzer() =
    inherit Analyzer()
    override this.createComponents(fieldName: string, reader: Reader) =
        let source = new KeywordTokenizer(reader)
        let result = new LowerCaseFilter(Constants.LuceneVersion, source)
        new org.apache.lucene.analysis.Analyzer.TokenStreamComponents(source, result)


// ----------------------------------------------------------------------------
// Indexing related message. The model could be considered similiar to
// Command–query separation pattern where are side effect free queries are 
// kept seperate from side effect based command. Also side effect operations
// don't support
// ----------------------------------------------------------------------------

/// Messages which can be send to the indexing queue to indicate the type of operation
/// and the associated data
type IndexCommand = 
    
    /// Create a new document
    | Create of id: string * fields: Dictionary<string,string>  
    
    /// Update an existing document
    | Update of id: string * fields: Dictionary<string,string>  
    
    /// Delete an existing document by id
    | Delete of id: string                     
    
    /// Optimistic concurrency controlled create of a document
    | OptimisticCreate of id: string * fields: Dictionary<string,string>
    
    /// Optimistic concurrency controlled update of a document
    | OptimisticUpdate of id: string * fields: Dictionary<string,string> * version: int
    
    /// Optimistic concurrency controlled delete of a document
    | OptimisticDelete of id: string * version: int
    
    /// Bulk delete all the documents in a index
    | BulkDeleteByIndexName               

    /// Commit pending index changes
    | Commit                              
    

type IndexQuery = 
    | SearchProfileQuery of SearchProfileQuery
    | SearchQuery of SearchQuery