namespace FlexSearch.Api

open FlexSearch.Api.Validation
open System
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.DataAnnotations
open System.Linq
open System.Runtime.Serialization

/// <summary>
/// Allows to control various Index Shards related settings.
/// </summary>
[<ToString; Sealed>]
type ShardConfiguration() = 
    inherit ValidatableObjectBase<ShardConfiguration>()
    /// <summary>
    /// Total number of shards to be present in the given index.
    /// </summary>
    [<DefaultValue(1); GreaterThanOrEqual(1)>]
    member val ShardCount = 1 with get, set

/// <summary>
/// Allows to control various Index related settings.
/// </summary>
[<ToString; Sealed>]
type IndexConfiguration() = 
    inherit ValidatableObjectBase<IndexConfiguration>()
    
    /// <summary>
    /// The amount of time in seconds that FlexSearch should wait before committing changes to the disk.
    /// </summary>
    [<DefaultValue(60)>]
    member val CommitTimeSeconds = 60 with get, set
    
    /// <summary>
    /// A Directory is a flat list of files. Files may be written once, when they are created. Once a 
    /// file is created it may only be opened for read, or deleted. Random access is permitted both 
    /// when reading and writing.
    /// </summary>
    [<DefaultValue(DirectoryType.MemoryMapped)>]
    member val DirectoryType = DirectoryType.MemoryMapped with get, set
    
    /// <summary>
    /// The default maximum time to wait for a write lock (in milliseconds).
    /// </summary>
    [<DefaultValue(1000)>]
    member val DefaultWriteLockTimeout = 1000 with get, set
    
    /// <summary>
    /// Determines the amount of RAM that may be used for buffering added documents and deletions 
    /// before they are flushed to the Directory.
    /// </summary>
    [<DefaultValue(100); GreaterThanOrEqual(20)>]
    member val RamBufferSizeMb = 100 with get, set
    
    /// <summary>
    /// The amount of time in milliseconds that FlexSearch should wait before reopening index reader. 
    /// This helps in keeping writing and real time aspects of the engine separate.
    /// </summary>
    [<DefaultValue(25); GreaterThanOrEqual(25)>]
    member val RefreshTimeMilliseconds = 25 with get, set
    
    /// <summary>
    /// Corresponds to Lucene Index version. There will
    /// always be a default codec associated with each index version.
    /// </summary>
    [<DefaultValue(IndexVersion.Lucene_4_10_1)>]
    member val IndexVersion = IndexVersion.Lucene_4_10_1 with get, set
    
    /// <summary>
    /// A postings format is responsible for encoding/decoding terms, postings, and proximity data.
    /// </summary>
    [<DefaultValue(FieldPostingsFormat.Bloom_4_1)>]
    member val IdFieldPostingsFormat = FieldPostingsFormat.Bloom_4_1 with get, set
    
    /// <summary>
    /// This will be computed at run time based on the index version
    /// </summary>
    [<Display(AutoGenerateField = false); IgnoreDataMember>]
    member val DefaultIndexPostingsFormat = Unchecked.defaultof<FieldPostingsFormat> with get, set
    
    /// <summary>
    /// Similarity defines the components of Lucene scoring. Similarity determines how Lucene weights terms,
    /// and Lucene interacts with Similarity at both index-time and query-time.
    /// </summary>
    [<DefaultValue(FieldSimilarity.TFIDF)>]
    member val DefaultFieldSimilarity = FieldSimilarity.TFIDF with get, set

/// <summary>
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
/// </summary>
[<ToString; Sealed>]
type Field(fieldName : string, fieldType : FieldType) = 
    inherit ValidatableObjectBase<Field>()
    
    /// <summary>
    /// Name of the field.
    /// </summary>
    [<Required; PropertyName>]
    member val FieldName = fieldName with get, set
    
    /// <summary>
    /// Signifies if the field should be analyzed using an analyzer. 
    /// </summary>
    [<DefaultValue(true)>]
    member val Analyze = true with get, set
    
    /// <summary>
    /// Signifies if a field should be indexed. A field can only be 
    /// stored without indexing.
    /// </summary>
    [<DefaultValue(true)>]
    member val Index = true with get, set
    
    /// <summary>
    /// Signifies if a field should be stored so that it can retrieved
    /// while searching.
    /// </summary>
    [<DefaultValue(true)>]
    member val Store = true with get, set
    
    /// <summary>
    /// Analyzer to be used while indexing.
    /// </summary>
    [<DefaultValue(StandardAnalyzer)>]
    member val IndexAnalyzer = StandardAnalyzer with get, set
    
    /// <summary>
    /// Analyzer to be used while searching.
    /// </summary>
    [<DefaultValue(StandardAnalyzer)>]
    member val SearchAnalyzer = StandardAnalyzer with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    [<DefaultValue(FieldType.Text)>]
    member val FieldType = fieldType with get, set
    
    /// <summary>
    ///  AUTO
    /// </summary>
    [<DefaultValue(FieldPostingsFormat.Lucene_4_1)>]
    member val PostingsFormat = FieldPostingsFormat.Lucene_4_1 with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    [<DefaultValue(FieldSimilarity.TFIDF)>]
    member val Similarity = FieldSimilarity.TFIDF with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    [<DefaultValue(FieldIndexOptions.DocsAndFreqsAndPositions)>]
    member val IndexOptions = FieldIndexOptions.DocsAndFreqsAndPositions with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    [<DefaultValue(FieldTermVector.DoNotStoreTermVector)>]
    member val TermVector = FieldTermVector.DoNotStoreTermVector with get, set
    
    /// <summary>
    /// If true, omits the norms associated with this field (this disables length 
    /// normalization and index-time boosting for the field, and saves some memory). 
    /// Defaults to true for all primitive (non-analyzed) field types, such as int, 
    /// float, data, bool, and string. Only full-text fields or fields that need an 
    /// index-time boost need norms.
    /// </summary>
    [<DefaultValue(true)>]
    member val OmitNorms = true with get, set
    
    /// <summary>
    /// Fields can get their content dynamically through scripts. This is the name of 
    /// the script to be used for getting field data at index time.
    /// </summary>
    member val ScriptName = "" with get, set
    
    new(fieldName : string) = Field(fieldName, FieldType.Text)
    new() = Field(Unchecked.defaultof<string>, FieldType.Text)
    override this.Validate(context) = 
        seq { 
            if (this.FieldType = FieldType.Text || this.FieldType = FieldType.Highlight 
                || this.FieldType = FieldType.Custom) 
               && (String.IsNullOrWhiteSpace(this.SearchAnalyzer) || String.IsNullOrWhiteSpace(this.IndexAnalyzer)) then 
                yield new ValidationResult("SearchAnalyzer and IndexAnalyzer are mandatory for Text, Highlight and Custom field types.")
        }

/// <summary>
/// Filters consume input and produce a stream of tokens. In most cases a filter looks 
/// at each token in the stream sequentially and decides whether to pass it along, 
/// replace it or discard it. A filter may also do more complex analysis by looking 
/// ahead to consider multiple tokens at once, although this is less common. 
/// </summary>
[<ToString; Sealed>]
type TokenFilter(filterName : string) = 
    inherit ValidatableObjectBase<TokenFilter>()
    
    /// <summary>
    /// The name of the filter. Some pre-defined filters are the following-
    /// + Ascii Folding Filter
    /// + Standard Filter
    /// + Beider Morse Filter
    /// + Double Metaphone Filter
    /// + Caverphone2 Filter
    /// + Metaphone Filter
    /// + Refined Soundex Filter
    /// + Soundex Filter
    /// + Keep Words Filter
    /// + Length Filter
    /// + Lower Case Filter
    /// + Pattern Replace Filter
    /// + Stop Filter
    /// + Synonym Filter
    /// + Reverse String Filter
    /// + Trim Filter
    /// For more details refer to http://flexsearch.net/docs/concepts/understanding-analyzers-tokenizers-and-filters/
    /// </summary>
    [<Required; PropertyName>]
    member val FilterName = filterName with get, set
    
    /// <summary>
    /// Parameters required by the filter.
    /// </summary>
    [<MinimumItems(1)>]
    member val Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    new() = TokenFilter(Unchecked.defaultof<string>)

/// <summary>
/// Tokenizer breaks up a stream of text into tokens, where each token is a sub-sequence
/// of the characters in the text. An analyzer is aware of the field it is configured 
/// for, but a tokenizer is not.
/// </summary>
[<ToString; Sealed>]
type Tokenizer(tokenizerName : string) = 
    inherit ValidatableObjectBase<Tokenizer>()
    
    /// <summary>
    /// The name of the tokenizer. Some pre-defined tokenizers are the following-
    /// + Standard Tokenizer
    /// + Classic Tokenizer
    /// + Keyword Tokenizer
    /// + Letter Tokenizer
    /// + Lower Case Tokenizer
    /// + UAX29 URL Email Tokenizer
    /// + White Space Tokenizer
    /// For more details refer to http://flexsearch.net/docs/concepts/understanding-analyzers-tokenizers-and-filters/
    /// </summary>
    [<Required; PropertyName>]
    member val TokenizerName = tokenizerName with get, set
    
    /// <summary>
    /// Parameters required by the tokenizer.
    /// </summary>
    member val Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    new() = Tokenizer(Unchecked.defaultof<_>)

/// <summary>
/// An analyzer examines the text of fields and generates a token stream.
/// </summary>
[<ToString; Sealed>]
type Analyzer() = 
    inherit ValidatableObjectBase<Analyzer>()
    
    /// <summary>
    /// Name of the analyzer
    /// </summary>
    [<Required>]
    member val AnalyzerName = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    [<Required; ValidateComplex>]
    member val Tokenizer = Unchecked.defaultof<Tokenizer> with get, set
    
    /// <summary>
    /// Filters to be used by the analyzer.
    /// </summary>
    [<Required; MinimumItems(1, ErrorMessage = "Analyzer requires at least 1 Filter to be defined.")>]
    member val Filters = new List<TokenFilter>() with get, set
    
    override this.Validate(context) = seq { yield Helpers.ValidateCollection<TokenFilter>(this.Filters) }

/// <summary>
/// Script is used to add scripting capability to the index. These can be used to generate dynamic
/// field values based upon other indexed values or to modify scores of the returned results.
/// Any valid C# expression can be used as a script.
/// </summary>
[<ToString; Sealed>]
type Script(scriptName : string, source : string, scriptType : ScriptType) = 
    inherit ValidatableObjectBase<Script>()
    
    /// <summary>
    /// Name of the script.
    /// </summary>
    [<Required; PropertyName>]
    member val ScriptName = scriptName with get, set
    
    /// <summary>
    /// Source code of the script. 
    /// </summary>
    [<Required>]
    member val Source = source with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val ScriptType = scriptType with get, set
    
    new() = Script(Unchecked.defaultof<string>, Unchecked.defaultof<string>, ScriptType.ComputedField)

/// <summary>
/// Used for configuring the settings for text highlighting in the search results
/// </summary>
[<ToString; Sealed>]
type HighlightOption(fields : List<string>) = 
    inherit ValidatableObjectBase<HighlightOption>()
    
    /// <summary>
    /// Total number of fragments to return per document
    /// </summary>
    [<DefaultValue(2)>]
    member val FragmentsToReturn = 2 with get, set
    
    /// <summary>
    /// The fields to be used for text highlighting
    /// </summary>
    member val HighlightedFields = fields with get, set
    
    /// <summary>
    /// Post tag to represent the ending of the highlighted word
    /// </summary>
    [<DefaultValue("</B>")>]
    member val PostTag = "</B>" with get, set
    
    /// <summary>
    /// Pre tag to represent the ending of the highlighted word
    /// </summary>
    [<DefaultValue("<B>")>]
    member val PreTag = "<B>" with get, set
    
    new() = HighlightOption(Unchecked.defaultof<List<string>>)

/// <summary>
/// Search query is used for searching over a FlexSearch index. This provides
/// a consistent syntax to execute various types of queries. The syntax is similar
/// to the SQL syntax. This was done on purpose to reduce the learning curve.
/// </summary>
[<ToString; Sealed>]
type SearchQuery(index : string, query : string) = 
    inherit ValidatableObjectBase<SearchQuery>()
    
    /// <summary>
    /// Unique name of the query. This is only required if you are setting up a 
    /// search profile.
    /// </summary>
    member val QueryName = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// Columns to be returned as part of results.
    /// + *  - return all columns
    /// + [] - return no columns
    /// + ["columnName"] -  return specific column
    /// </summary>
    member val Columns = new List<string>() with get, set
    
    /// <summary>
    /// Count of results to be returned
    /// </summary>
    [<DefaultValue(10)>]
    member val Count = 10 with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val Highlights = Unchecked.defaultof<HighlightOption> with get, set
    
    /// <summary>
    /// Name of the index
    /// </summary>
    [<Required>]
    member val IndexName = index with get, set
    
    /// <summary>
    /// Can be used to order the results by score or specific field.
    /// </summary>
    [<DefaultValue("score")>]
    member val OrderBy = "score" with get, set
    
    /// <summary>
    /// Used to enable paging and skip certain pre-fetched results.
    /// </summary>
    [<DefaultValue(0)>]
    member val Skip = 0 with get, set
    
    /// <summary>
    /// Query string to be used for searching
    /// </summary>
    [<Required>]
    member val QueryString = query with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val MissingValueConfiguration = new Dictionary<string, MissingValueOption>(StringComparer.OrdinalIgnoreCase) with get, set
    
    /// <summary>
    /// Universal configuration for the missing field values. Only applicable
    /// for search profiles.
    /// </summary>
    [<DefaultValue(MissingValueOption.Default)>]
    member val GlobalMissingValue = MissingValueOption.Default with get, set
    
    /// <summary>
    /// If true will return collapsed search results which are in tabular form.
    /// Flat results enable easy binding to a grid but grouping results is tougher
    /// with Flat result.
    /// </summary>
    [<DefaultValue(false)>]
    member val ReturnFlatResult = false with get, set
    
    /// <summary>
    /// If true then scores are returned as a part of search result.
    /// </summary>
    [<DefaultValue(true)>]
    member val ReturnScore = true with get, set
    
    /// <summary>
    /// Profile Name to be used for profile based searching.
    /// </summary>
    member val SearchProfile = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// Script which can be used to select a search profile. This can help in
    /// dynamic selection of search profile based on the incoming data.
    /// </summary>
    member val SearchProfileSelector = Unchecked.defaultof<string> with get, set
    
    new() = SearchQuery(Unchecked.defaultof<_>, Unchecked.defaultof<_>)

/// <summary>
/// Represents the search result document returned by FlexSearch.
/// </summary>
[<ToString; Sealed>]
type ResultDocument(indexName : string, id : string) = 
    inherit ValidatableObjectBase<Script>()
    
    /// <summary>
    /// Fields which are part of the returned document
    /// </summary>
    member val Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    /// <summary>
    /// Any matched text highlighted snippets.
    /// </summary>
    member val Highlights = new List<string>() with get, set
    
    /// <summary>
    /// Score of the returned document.
    /// </summary>
    [<DefaultValue(0.0)>]
    member val Score = 0.0 with get, set
    
    /// <summary>
    /// Unique Id.
    /// </summary>
    [<Required>]
    member val Id = id with get, set
    
    /// <summary>
    /// Timestamp of the last modification of the document
    /// </summary>
    member val TimeStamp = Unchecked.defaultof<Int64> with get, set
    
    /// <summary>
    /// Name of the index
    /// </summary>
    [<Required>]
    member val IndexName = indexName with get, set
    
    new() = ResultDocument(Unchecked.defaultof<string>, Unchecked.defaultof<string>)

/// <summary>
/// A document represents the basic unit of information which can be added or retrieved from the index. 
/// A document consists of several fields. A field represents the actual data to be indexed. In database 
/// analogy an index can be considered as a table while a document is a row of that table. Like a table a 
/// FlexSearch document requires a fix schema and all fields should have a field type.
/// </summary>
[<ToString; Sealed>]
type FlexDocument(indexName : string, id : string) = 
    inherit ValidatableObjectBase<Script>()
    
    /// <summary>
    /// Fields to be added to the document for indexing.
    /// </summary>
    /// <remarks>
    /// Field names should be unique.
    /// </remarks>
    member val Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    /// <summary>
    /// Unique Id of the document
    /// </summary>
    [<Required>]
    member val Id = id with get, set
    
    /// <summary>
    /// Timestamp of the last modification of the document. This field is interpreted differently
    /// during a create and update operation. It also dictates whether and unique Id check is to be performed
    ///  or not. 
    /// Version number semantics
    /// + 0 - Don't care about the version and proceed with the operation normally.
    /// + -1 - Ensure that the document does not exist (Performs unique Id check).
    /// + 1 - Ensure that the document does exist. This is not relevant for create operation.
    /// > 1 - Ensure that the version matches exactly. This is not relevant for create operation.
    /// </summary>
    member val TimeStamp = Unchecked.defaultof<Int64> with get, set
    
    /// <summary>
    /// Name of the index
    /// </summary>
    [<Required>]
    member val IndexName = indexName with get, set
    
    new() = FlexDocument(Unchecked.defaultof<string>, Unchecked.defaultof<string>)

/// <summary>
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
/// </summary>
[<ToString; Sealed>]
type Index() = 
    inherit ValidatableObjectBase<Index>()
    
    /// <summary>
    /// Name of the index
    /// </summary>
    [<PropertyName; Required(AllowEmptyStrings = false)>]
    member val IndexName = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// Fields to be used in index.
    /// </summary>
    member val Fields = new List<Field>() with get, set
    
    /// <summary>
    /// Scripts to be used in index.
    /// </summary>
    member val Scripts = new List<Script>() with get, set
    
    /// <summary>
    /// Search Profiles
    /// </summary>
    member val SearchProfiles = new List<SearchQuery>() with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val ShardConfiguration = new ShardConfiguration() with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val IndexConfiguration = new IndexConfiguration() with get, set
    
    /// <summary>
    /// Signifies if the index is on-line or not? An index has to be 
    /// on-line in order to enable searching over it.
    /// </summary>
    [<DefaultValue(false)>]
    member val Online = false with get, set
    
    override this.Validate(context) = 
        seq { 
            yield Helpers.ValidateCollection<Field>(this.Fields)
            yield Helpers.ValidateCollection<Script>(this.Scripts)
            yield Helpers.ValidateCollection<SearchQuery>(this.SearchProfiles)
            // Check for duplicate field names
            let duplicateFieldNames = 
                query { 
                    for field in this.Fields do
                        groupBy field.FieldName into g
                        where (g.Count() > 1)
                        select g.Key
                }
            if duplicateFieldNames.Count() <> 0 then 
                yield new ValidationResult(Errors.DUPLICATE_FIELD_VALUE |> AppendKv("Field", "FieldName"))
            // Check for duplicate scripts
            let duplicateScriptNames = 
                query { 
                    for script in this.Scripts do
                        groupBy script.ScriptName into g
                        where (g.Count() > 1)
                        select g.Key
                }
            if duplicateScriptNames.Count() <> 0 then 
                yield new ValidationResult(Errors.DUPLICATE_FIELD_VALUE |> AppendKv("Field", "ScriptName"))
            // Check if any query name is missing in search profiles. Cannot do this through annotation as the
            // Query Name is not mandatory for normal Search Queries
            let missingQueryNames = this.SearchProfiles |> Seq.filter (fun x -> String.IsNullOrWhiteSpace(x.QueryName))
            if missingQueryNames.Count() <> 0 then 
                yield new ValidationResult(Errors.MISSING_FIELD_VALUE
                                           |> AppendKv("Field", "QueryName")
                                           |> AppendKv("Message", "QueryName is required."))
            // Check for duplicate Search queries
            let duplicateSearchQueries = 
                query { 
                    for script in this.SearchProfiles do
                        groupBy script.QueryName into g
                        where (g.Count() > 1)
                        select g.Key
                }
            if duplicateSearchQueries.Count() <> 0 then 
                yield new ValidationResult(Errors.DUPLICATE_FIELD_VALUE |> AppendKv("Field", "QueryName"))
            for field in this.Fields do
                // Check if the specified script exists
                if String.IsNullOrWhiteSpace(field.ScriptName) = false then 
                    if this.Scripts.FirstOrDefault(fun x -> x.ScriptName = field.ScriptName) = Unchecked.defaultof<Script> then 
                        yield new ValidationResult(Errors.SCRIPT_NOT_FOUND |> AppendKv("ScriptName", field.ScriptName))
                    else ()
        }

/// <summary>
/// Represents the result returned by FlexSearch for a given search query.
/// </summary>
[<ToString; Sealed>]
type SearchResults() = 
    
    /// <summary>
    /// DOcuments which are returned as a part of search response.
    /// </summary>
    member val Documents = new List<ResultDocument>() with get, set
    
    /// <summary>
    /// Total number of records returned.
    /// </summary>
    [<DefaultValue(0)>]
    member val RecordsReturned = 0 with get, set
    
    /// <summary>
    /// Total number of records available on the server. This could be 
    /// greater than the returned results depending upon the requested 
    /// document count. 
    /// </summary>
    [<DefaultValue(0)>]
    member val TotalAvailable = 0 with get, set

/// <summary>
/// Represents a list of words which can be used for filtering by an analyzer.
/// These can contain stop words or keep words etc.
/// </summary>
[<ToString; Sealed>]
type FilterList(words : List<string>) = 
    inherit ValidatableObjectBase<FilterList>()
    
    /// <summary>
    /// List of words
    /// </summary>
    [<MinimumItems(1)>]
    member val Words = words with get, set
    
    new() = FilterList(Unchecked.defaultof<List<string>>)

/// <summary>
/// Represents a list of words which can be used for synonym matching by an analyzer.
/// </summary>
[<ToString; Sealed>]
type MapList(words : Dictionary<string, List<string>>) = 
    inherit ValidatableObjectBase<MapList>()
    
    /// <summary>
    /// Words to be used for synonym matching.
    /// </summary>
    [<MinimumItems(1)>]
    member val Words = words with get, set
    
    new() = MapList(new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase))

/// <summary>
/// Used by long running processes. All long running FlexSearch operations create
/// an instance of Job and return the Id to the caller. This Id can be used by the
/// caller to check the status of the job.
///
/// NOTE: Job information is not persistent
/// </summary>
[<ToString; Sealed>]
type Job() = 
    inherit ValidatableObjectBase<Job>()
    
    /// <summary>
    /// Unique Id of the Job
    /// </summary>
    member val JobId = "" with get, set
    
    /// <summary>
    /// Total items to be processed as a part of the current job.
    /// </summary>
    [<DefaultValue(0)>]
    member val TotalItems = 0 with get, set
    
    /// <summary>
    /// Items already processed.
    /// </summary>
    [<DefaultValue(0)>]
    member val ProcessedItems = 0 with get, set
    
    /// <summary>
    /// Items which have failed processing.
    /// </summary>
    [<DefaultValue(0)>]
    member val FailedItems = 0 with get, set
    
    /// <summary>
    /// Overall status of the job.
    /// </summary>
    [<DefaultValue(JobStatus.Initializing)>]
    member val Status = JobStatus.Initializing with get, set
    
    /// <summary>
    /// Any message that is associated with the job.
    /// </summary>
    member val Message = "" with get, set
//[<ToString; Sealed>]
//type ImportRequest() = 
//    inherit ValidatableObjectBase<ImportRequest>()
//    member val Id = "" with get, set
//    member val Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
//    
//    [<DefaultValue(false)>]
//    member val ForceCreate = false with get, set
//    
//    member val JobId = "" with get, set
//
//[<ToString; Sealed>]
//type ImportResponse() = 
//    member val JobId = "" with get, set
//    member val Message = "" with get, set
