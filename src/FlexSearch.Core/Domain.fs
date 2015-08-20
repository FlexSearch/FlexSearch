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
open System.Collections.Generic
open System
open System.Linq

/// Similarity defines the components of Lucene scoring. Similarity 
/// determines how Lucene weights terms, and Lucene interacts with 
/// Similarity at both index-time and query-time.
type FieldSimilarity = 
    | Undefined = 0
    | BM25 = 1
    | TFIDF = 2

/// A Directory is a flat list of files. Files may be written once, when they are created. 
/// Once a file is created it may only be opened for read, or deleted. Random access is 
/// permitted both when reading and writing.
type DirectoryType = 
    | Undefined = 0
    | FileSystem = 1
    | MemoryMapped = 2
    | Ram = 3

/// Signifies Shard status
type ShardStatus = 
    | Undefined = 0
    | Opening = 1
    | Recovering = 2
    | Online = 3
    | Offline = 4
    | Closing = 5
    | Faulted = 6

/// Represents the current state of the index.
type IndexStatus = 
    | Undefined = 0
    | Opening = 1
    | Recovering = 2
    | Online = 3
    | OnlineFollower = 4
    | Offline = 5
    | Closing = 6
    | Faulted = 7

/// These options instruct FlexSearch to maintain full term vectors for each document, 
/// optionally including the position and offset information for each term occurrence 
/// in those vectors. These can be used to accelerate highlighting and other ancillary 
/// functionality, but impose a substantial cost in terms of index size. These can 
/// only be configured for custom field type.
type FieldTermVector = 
    | Undefined = 0
    /// Do not store term vectors.
    | DoNotStoreTermVector = 1
    /// Store the term vectors of each document. A term vector is a list of the 
    /// document's terms and their number of occurrences in that document.
    | StoreTermVector = 2
    /// Store the term vector and token position information
    | StoreTermVectorsWithPositions = 3
    /// Store the term vector, Token position and offset information
    | StoreTermVectorsWithPositionsandOffsets = 4

/// Controls how much information is stored in the postings lists.
type FieldIndexOptions = 
    | Undefined = 0
    /// Only documents are indexed: term frequencies and positions are omitted.
    | DocsOnly = 1
    /// Only documents and term frequencies are indexed: positions are omitted.
    | DocsAndFreqs = 2
    /// Indexes documents, frequencies and positions
    | DocsAndFreqsAndPositions = 3
    /// Indexes documents, frequencies, positions and offsets.
    | DocsAndFreqsAndPositionsAndOffsets = 4

/// Corresponds to Lucene Index version. There will
/// always be a default codec associated with each index version.
type IndexVersion = 
    | Undefined = 0
    /// Lucene 4.x.x index format
    /// It is deprecated and is here for legacy support
    | Lucene_4_x_x = 1
    /// Lucene 5.0.0 index format
    | Lucene_5_0_0 = 2

/// Represents the status of job.
type JobStatus = 
    | Undefined = 0
    /// Job is currently initializing. This essentially means that the job is added to the 
    /// job queue but has started executing. Depending upon the type of connector a job can take
    /// a while to move from this status. This also depends upon the parallel capability of the
    // connector. Most of the connectors are designed to perform one operation at a time. 
    | Initializing = 1
    /// Job is initialized. All the parameters supplied as a part of the job are correct and job has 
    /// been scheduled successfully.
    | Initialized = 2
    /// Job is currently being executed by the engine. 
    | InProgress = 3
    /// Job has finished without errors.
    | Completed = 4
    /// Job has finished with errors. Check the message property to get error details. Jobs have access to
    /// the engine's logging service so they could potentially write more error information to the logs.
    | CompletedWithErrors = 5

/// The field type defines how FlexSearch should interpret data in a field and how the 
/// field can be queried. There are many field types included with FlexSearch by default, 
/// and custom types can also be defined.
type FieldDataType = 
    // Case to handle deserialization to default value
    | Undefined = 0
    /// Integer
    | Int = 1
    /// Double
    | Double = 2
    /// Field to store keywords. The entire input will be treated as a single word. This is 
    /// useful for fields like customerid, referenceid etc. These fields only support complete 
    /// text matching while searching and no partial word match is available.
    | ExactText = 3
    /// General purpose field to store normal textual data
    | Text = 4
    /// Similar to Text field but supports highlighting of search results
    | Highlight = 5
    /// Boolean
    | Bool = 6
    /// Fixed format date field (Supported format: YYYYmmdd)
    | Date = 7
    /// Fixed format datetime field (Supported format: YYYYMMDDhhmmss)
    | DateTime = 8
    /// Custom field type which gives more granular control over the field configuration
    | Custom = 9
    /// Non-indexed field. Only used for retrieving stored text. Searching is not
    /// possible over these fields.
    | Stored = 10
    /// Long
    | Long = 11


/// Allows to control various Index Shards related settings.
[<ToString; Sealed>]
type ShardConfiguration() = 
    inherit DtoBase()
    
    /// Total number of shards to be present in the given index.
    member val ShardCount = 1 with get, set
    
    override this.Validate() = this.ShardCount |> gt ("ShardCount") 1

/// Allows to control various Index related settings.
[<ToString; Sealed>]
type IndexConfiguration() = 
    inherit DtoBase()
    
    /// The amount of time in seconds that FlexSearch 
    /// should wait before committing changes to the disk.
    /// This is only used if no commits have happended in the
    /// set time period otherwise CommitEveryNFlushes takes care
    /// of commits
    member val CommitTimeSeconds = 60 with get, set
    
    /// Determines how often the data be committed to the
    /// physical medium. Commits are more expensive then
    /// flushes so keep the setting as high as possilbe. Making
    /// this setting too high will result in excessive ram usage.  
    member val CommitEveryNFlushes = 3 with get, set
    
    /// Determines whether to commit first before closing an index
    member val CommitOnClose = true with get, set
    
    /// Determines whether to enable auto commit functionality or not
    member val AutoCommit = true with get, set
    
    /// A Directory is a flat list of files. Files may be 
    /// written once, when they are created. Once a file 
    /// is created it may only be opened for read, or 
    /// deleted. Random access is permitted both when 
    /// reading and writing.
    member val DirectoryType = DirectoryType.MemoryMapped with get, set
    
    /// The default maximum time to wait for a write 
    /// lock (in milliseconds).
    member val DefaultWriteLockTimeout = 1000 with get, set
    
    /// Determines the amount of RAM that may be used 
    /// for buffering added documents and deletions 
    /// before they are flushed to the Directory.
    member val RamBufferSizeMb = 100 with get, set
    
    /// The number of buffered added documents that will 
    /// trigger a flush if enabled.
    member val MaxBufferedDocs = -1 with get, set
    
    /// The amount of time in milliseconds that FlexSearch 
    /// should wait before reopening index reader. This 
    /// helps in keeping writing and real time aspects of 
    /// the engine separate.
    member val RefreshTimeMilliseconds = 500 with get, set
    
    /// Determines whether to enable auto refresh or not
    member val AutoRefresh = true with get, set
    
    /// Corresponds to Lucene Index version. There will
    /// always be a default codec associated with each 
    /// index version.
    member val IndexVersion = IndexVersion.Lucene_5_0_0 with get, set
    
    /// Signifies if bloom filter should be used for 
    /// encoding Id field.
    member val UseBloomFilterForId = true with get, set
    
    /// Similarity defines the components of Lucene scoring. Similarity
    /// determines how Lucene weights terms and Lucene interacts with 
    /// Similarity at both index-time and query-time.
    member val DefaultFieldSimilarity = FieldSimilarity.TFIDF with get, set
    
    override this.Validate() = this.CommitTimeSeconds
                               |> gte "CommitTimeSeconds" 30
                               >>= (fun _ -> this.MaxBufferedDocs |> gte "MaxBufferedDocs" 2)
                               >>= (fun _ -> this.RamBufferSizeMb |> gte "RamBufferSizeMb" 20)
                               >>= (fun _ -> this.RefreshTimeMilliseconds |> gte "RefreshTimeMilliseconds" 25)


/// Filters consume input and produce a stream of tokens. In most cases a filter looks 
/// at each token in the stream sequentially and decides whether to pass it along, 
/// replace it or discard it. A filter may also do more complex analysis by looking 
/// ahead to consider multiple tokens at once, although this is less common. 
[<ToString; Sealed>]
type TokenFilter() = 
    inherit DtoBase()
    
    /// The name of the filter. Some pre-defined filters are the following-
    /// + Ascii Folding Filter
    /// + Standard Filter
    /// + Beider Morse Filter
    /// + Double Metaphone Filter
    /// + Caverphone2 Filter
    /// + Metaphone Filter
    /// + Refined Soundex Filter
    /// + Soundex Filter
    /// For more details refer to http://flexsearch.net/docs/concepts/understanding-analyzers-tokenizers-and-filters/
    member val FilterName = defString with get, set
    
    /// Parameters required by the filter.
    member val Parameters = strDict() with get, set
    
    override this.Validate() = this.FilterName |> propertyNameValidator "FilterName"

/// Tokenizer breaks up a stream of text into tokens, where each token is a sub-sequence
/// of the characters in the text. An analyzer is aware of the field it is configured 
/// for, but a tokenizer is not.
[<ToStringAttribute; Sealed>]
type Tokenizer() = 
    inherit DtoBase()
    
    /// The name of the tokenizer. Some pre-defined tokenizers are the following-
    /// + Standard Tokenizer
    /// + Classic Tokenizer
    /// + Keyword Tokenizer
    /// + Letter Tokenizer
    /// + Lower Case Tokenizer
    /// + UAX29 URL Email Tokenizer
    /// + White Space Tokenizer
    /// For more details refer to http://flexsearch.net/docs/concepts/understanding-analyzers-tokenizers-and-filters/
    member val TokenizerName = "standard" with get, set
    
    /// Parameters required by the tokenizer.
    member val Parameters = strDict() with get, set
    
    override this.Validate() = this.TokenizerName |> propertyNameValidator "TokenizerName"

/// An analyzer examines the text of fields and generates a token stream.
[<ToStringAttribute; Sealed>]
type Analyzer() = 
    inherit DtoBase()
    
    /// Name of the analyzer
    member val AnalyzerName = defString with get, set
    
    // AUTO
    member val Tokenizer = new Tokenizer() with get, set
    
    /// Filters to be used by the analyzer.
    member val Filters = new List<TokenFilter>() with get, set
    
    override this.Validate() = this.AnalyzerName
                               |> propertyNameValidator "AnalyzerName"
                               >>= this.Tokenizer.Validate
                               >>= fun _ -> seqValidator (this.Filters.Cast<DtoBase>())

/// A field is a section of a Document. 
/// <para>
/// Fields can contain different kinds of data. A name field, for example, 
/// is text (character data). A shoe size field might be a floating point number 
/// so that it could contain values like 6 and 9.5. Obviously, the definition of 
/// fields is flexible (you could define a shoe size field as a text field rather
/// than a floating point number, for example), but if you define your fields correctly, 
/// FlexSearch will be able to interpret them correctly and your users will get better 
/// results when they perform a query.
/// </para>
/// <para>
/// You can tell FlexSearch about the kind of data a field contains by specifying its 
/// field type. The field type tells FlexSearch how to interpret the field and how 
/// it can be queried. When you add a document, FlexSearch takes the information in 
/// the document’s fields and adds that information to an index. When you perform a 
/// query, FlexSearch can quickly consult the index and return the matching documents.
/// </para>
[<ToString; Sealed>]
type Field(fieldName : string, fieldType : FieldDataType) = 
    inherit DtoBase()
    
    /// Name of the field.
    member val FieldName = fieldName with get, set
    
    /// Signifies if the field should be analyzed using an analyzer. 
    member val Analyze = true with get, set
    
    /// Signifies if a field should be indexed. A field can only be 
    /// stored without indexing.
    member val Index = true with get, set
    
    /// Signifies if a field should be stored so that it can retrieved
    /// while searching.
    member val Store = true with get, set
    
    /// If AllowSort is set to true then we will index the field with docvalues.
    member val AllowSort = false with get, set
    
    /// If AllowFaceting is set to true then we will index the field with sorted set docvalues
    member val AllowFaceting = false with get, set

    /// Analyzer to be used while indexing.
    member val IndexAnalyzer = StandardAnalyzer with get, set
    
    /// Analyzer to be used while searching.
    member val SearchAnalyzer = StandardAnalyzer with get, set
    
    /// AUTO
    member val FieldType = fieldType with get, set
    
    /// AUTO
    member val Similarity = FieldSimilarity.TFIDF with get, set
    
    /// AUTO
    member val IndexOptions = FieldIndexOptions.DocsAndFreqsAndPositions with get, set
    
    /// AUTO
    member val TermVector = FieldTermVector.DoNotStoreTermVector with get, set
    
    /// If true, omits the norms associated with this field (this disables length 
    /// normalization and index-time boosting for the field, and saves some memory). 
    /// Defaults to true for all primitive (non-analyzed) field types, such as int, 
    /// float, data, bool, and string. Only full-text fields or fields that need an 
    /// index-time boost need norms.
    member val OmitNorms = true with get, set
    
    /// Fields can get their content dynamically through scripts. This is the name of 
    /// the script to be used for getting field data at index time.
    /// Script name follows the below convention
    /// ScriptName('param1','param2','param3')
    member val ScriptName = "" with get, set
    
    new(fieldName : string) = Field(fieldName, FieldDataType.Text)
    new() = Field(defString, FieldDataType.Text)
    override this.Validate() = 
        this.FieldName
        |> propertyNameValidator "FieldName"
        >>= (fun _ -> 
        if (this.FieldType = FieldDataType.Text || this.FieldType = FieldDataType.Highlight 
            || this.FieldType = FieldDataType.Custom) 
           && (String.IsNullOrWhiteSpace(this.SearchAnalyzer) || String.IsNullOrWhiteSpace(this.IndexAnalyzer)) then 
            fail (AnalyzerIsMandatory(this.FieldName))
        else okUnit)

/// Used for configuring the settings for text highlighting in the search results
[<ToString; Sealed>]
type HighlightOption(fields : string []) = 
    inherit DtoBase()
    
    /// Total number of fragments to return per document
    member val FragmentsToReturn = 2 with get, set
    
    /// The fields to be used for text highlighting
    member val HighlightedFields = fields with get, set
    
    /// Post tag to represent the ending of the highlighted word
    member val PostTag = "</B>" with get, set
    
    /// Pre tag to represent the ending of the highlighted word
    member val PreTag = "<B>" with get, set
    
    new() = HighlightOption(Unchecked.defaultof<string []>)
    override __.Validate() = okUnit

/// Search query is used for searching over a FlexSearch index. This provides
/// a consistent syntax to execute various types of queries. The syntax is similar
/// to the SQL syntax. This was done on purpose to reduce the learning curve.
[<ToString; Sealed>]
type SearchQuery(index : string, query : string) = 
    inherit DtoBase()
    
    /// Unique name of the query. This is only required if you are setting up a 
    /// search profile.
    member val QueryName = defString with get, set
    
    /// Columns to be returned as part of results.
    /// + *  - return all columns
    /// + [] - return no columns
    /// + ["columnName"] -  return specific column
    member val Columns = Array.empty<string> with get, set
    
    /// Count of results to be returned
    member val Count = 10 with get, set
    
    /// AUTO
    member val Highlights = new HighlightOption(Array.empty) with get, set
    
    /// Name of the index
    member val IndexName = index with get, set
    
    /// Can be used to order the results by score or specific field.
    member val OrderBy = "score" with get, set
    
    /// Can be used to determine the sort order.
    member val OrderByDirection = "asc" with get, set
    
    /// Can be used to remove results lower than a certain threshold.
    /// This works in conjunction with the score of the top record as
    /// all the other records are filtered using the score set by the
    /// top scoring record.
    member val CutOff = defDouble with get, set
    
    /// Can be used to return records with distinct values for 
    /// the given field. Works in a manner similar to Sql distinct by clause.
    member val DistinctBy = defString with get, set
    
    /// Used to enable paging and skip certain pre-fetched results.
    member val Skip = 0 with get, set
    
    /// Query string to be used for searching
    member val QueryString = query with get, set
    
    /// If true will return collapsed search results which are in tabular form.
    /// Flat results enable easy binding to a grid but grouping results is tougher
    /// with Flat result.
    member val ReturnFlatResult = false with get, set
    
    /// If true then scores are returned as a part of search result.
    member val ReturnScore = true with get, set
    
    /// Profile Name to be used for profile based searching.
    member val SearchProfile = defString with get, set
    
    /// Script which can be used to select a search profile. This can help in
    /// dynamic selection of search profile based on the incoming data.
    member val SearchProfileScript = defString with get, set
    
    /// Can be used to override the configuration saved in the search profile
    /// with the one which is passed as the Search Query
    member val OverrideProfileOptions = false with get, set
    
    /// Returns an empty string for null values saved in the index rather than
    /// the null constant
    member val ReturnEmptyStringForNull = true with get, set
    
    new() = SearchQuery(defString, defString)
    override this.Validate() = this.IndexName |> propertyNameValidator "IndexName"

    
/// A document represents the basic unit of information which can be added or retrieved from the index. 
/// A document consists of several fields. A field represents the actual data to be indexed. In database 
/// analogy an index can be considered as a table while a document is a row of that table. Like a table a 
/// FlexSearch document requires a fix schema and all fields should have a field type.
[<ToString; Sealed>]
type Document(indexName : string, id : string) = 
    inherit DtoBase()
    
    /// Fields to be added to the document for indexing.
    member val Fields = defStringDict() with get, set
    
    /// Unique Id of the document
    member val Id = id with get, set
    
    /// Timestamp of the last modification of the document. This field is interpreted differently
    /// during a create and update operation. It also dictates whether and unique Id check is to be performed
    ///  or not. 
    /// Version number semantics
    /// + 0 - Don't care about the version and proceed with the operation normally.
    /// + -1 - Ensure that the document does not exist (Performs unique Id check).
    /// + 1 - Ensure that the document does exist. This is not relevant for create operation.
    /// > 1 - Ensure that the version matches exactly. This is not relevant for create operation.
    member val TimeStamp = defInt64 with get, set
    
    /// mutable ModifyIndex : Int64
    /// Name of the index
    member val IndexName = indexName with get, set
    
    /// Any matched text highlighted snippets. Note: Only used for results
    member val Highlights = defStringList with get, set
    
    /// Score of the returned document. Note: Only used for results
    member val Score = defDouble with get, set
    
    static member Default = 
        let def = new Document()
        (def :> IFreezable).Freeze()
        def
    
    override this.Validate() = this.IndexName
                               |> notBlank "IndexName"
                               >>= fun _ -> this.Id |> notBlank "Id"
    new(indexName, id) = Document(indexName, id)
    new() = Document(defString, defString)

/// FlexSearch index is a logical index built on top of Lucene’s index in a manner 
/// to support features like schema and sharding. So in this sense a FlexSearch 
/// index consists of multiple Lucene’s index. Also, each FlexSearch shard is a valid 
/// Lucene index.
///
/// In case of a database analogy an index represents a table in a database where 
/// one has to define a schema upfront before performing any kind of operation on 
/// the table. There are various properties that can be defined at the index creation 
/// time. Only IndexName is a mandatory property, though one should always define 
/// Fields in an index to make any use of it.
///
/// By default a newly created index stays off-line. This is by design to force the 
/// user to enable an index before using it.
[<ToString; Sealed>]
type Index() = 
    inherit DtoBase()
    
    /// Name of the index
    member val IndexName = defString with get, set
    
    /// Fields to be used in index.
    member val Fields = defArray<Field> with get, set
    
    /// Search Profiles
    member val SearchProfiles = defArray<SearchQuery> with get, set
    
    /// AUTO
    member val ShardConfiguration = new ShardConfiguration() with get, set
    
    /// AUTO
    member val IndexConfiguration = new IndexConfiguration() with get, set
    
    /// Signifies if the index is on-line or not? An index has to be 
    /// on-line in order to enable searching over it.
    member val Active = true with get, set
    
    override this.Validate() = 
        let checkDuplicateFieldName() = 
            this.Fields.Select(fun x -> x.FieldName).ToArray() |> hasDuplicates "Fields" "FieldName"
        let checkDuplicateQueries() = 
            this.SearchProfiles.Select(fun x -> x.QueryName).ToArray() |> hasDuplicates "SearchProfiles" "QueryName"
        
        let validateSearchQuery() = 
            // Check if any query name is missing in search profiles. Cannot do this through annotation as the
            // Query Name is not mandatory for normal Search Queries
            let missingQueryNames = this.SearchProfiles |> Seq.filter (fun x -> String.IsNullOrWhiteSpace(x.QueryName))
            if missingQueryNames.Count() <> 0 then fail (NotBlank("QueryName"))
            else okUnit
        this.IndexName
        |> propertyNameValidator "IndexName"
        >>= fun _ -> seqValidator (this.Fields.Cast<DtoBase>())
        >>= fun _ -> seqValidator (this.SearchProfiles.Cast<DtoBase>())
        >>= checkDuplicateFieldName
        >>= validateSearchQuery
        >>= checkDuplicateQueries

//////////////////////////////////////////////////////////////////////////
/// Helper DTOs
//////////////////////////////////////////////////////////////////////////
/// Represents the result returned by FlexSearch for a given search query.
[<ToString; Sealed>]
type SearchResults() = 
    inherit DtoBase()

    /// Documents which are returned as a part of search response.
    member val Documents = new List<Document>() with get, set
    
    /// Total number of records returned.
    member val RecordsReturned = 0 with get, set
    
    /// Total number of records available on the server. This could be 
    /// greater than the returned results depending upon the requested 
    /// document count. 
    member val TotalAvailable = 0 with get, set

    override this.Validate() = okUnit

/// Used by long running processes. All long running FlexSearch operations create
/// an instance of Job and return the Id to the caller. This Id can be used by the
/// caller to check the status of the job.
///
/// NOTE: Job information is not persistent
[<ToString; Sealed>]
type Job() = 
    inherit DtoBase()
    
    /// Unique Id of the Job
    member val JobId = defString with get, set
    
    /// Total items to be processed as a part of the current job.
    member val TotalItems = 0 with get, set
    
    /// Items already processed.
    member val ProcessedItems = 0 with get, set
    
    /// Items which have failed processing.
    member val FailedItems = 0 with get, set
    
    /// Overall status of the job.
    member val Status = JobStatus.Initializing with get, set
    
    /// Any message that is associated with the job.
    member val Message = defString with get, set
    
    override __.Validate() = okUnit

/// <summary>
/// Request to analyze a text against an analyzer. The reason to force
/// this parameter to request body is to avoid escaping of restricted characters
/// in the uri.
/// This is helpful during analyzer testing.
/// </summary>
[<ToString; Sealed>]
type AnalysisRequest() = 
    inherit DtoBase()
    member val Text = defString with get, set
    override this.Validate() = 
        if this.Text |> isBlank then MissingFieldValue "Text" |> fail
        else okUnit

[<ToString; Sealed>]
type CreateResponse(id : string) =
    inherit DtoBase() 
    member val Id = id with get, set
    override this.Validate() = okUnit
    new() = CreateResponse("")

[<ToString; Sealed>]
type IndexExistsResponse() = 
    inherit DtoBase()
    member val Exists = Unchecked.defaultof<bool> with get, set
    override this.Validate() = okUnit

[<ToString; Sealed>]
type MemoryDetailsResponse() = 
    inherit DtoBase()
    // Memory used by FlexSearch application
    member val UsedMemory = defInt64 with get, set
    // Total available memory for the server
    member val TotalMemory = 0UL with get, set
    // Percentage of memory used by FlexSearch
    member val Usage = defDouble with get, set
    override this.Validate() = okUnit

type NoBody() = 
    inherit DtoBase()
    override __.Validate() = okUnit

type SearchProfileTestDto() =
    inherit DtoBase()
    member val SearchQuery = Unchecked.defaultof<SearchQuery> with get, set
    member val SearchProfile = defString with get, set
    override this.Validate() = 
        this.SearchQuery.Validate()
        >>= fun _ -> notBlank "SearchProfile" this.SearchProfile