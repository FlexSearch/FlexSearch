namespace FlexSearch.Api

open System.ComponentModel.DataAnnotations
open System.ComponentModel
open System
open System.Collections.Generic
open FlexSearch.Api.Validation
open NullGuard

[<ToString>]
type ShardConfiguration() = 
    inherit ValidatableObjectBase<ShardConfiguration>()
    [<DefaultValue(1); GreaterThanOrEqual(1)>]
    member val ShardCount = 1 with get, set

[<ToString>]
type IndexConfiguration() = 
    inherit ValidatableObjectBase<IndexConfiguration>()
    
    [<DefaultValue(60)>]
    member val CommitTimeSeconds = 60 with get, set
    
    member val DirectoryType = DirectoryType.MemoryMapped with get, set
    
    [<DefaultValue(1000)>]
    member val DefaultWriteLockTimeout = 1000 with get, set
    
    [<DefaultValue(100); GreaterThanOrEqual(20)>]
    member val RamBufferSizeMb = 100 with get, set
    
    [<DefaultValue(25); GreaterThanOrEqual(25)>]
    member val RefreshTimeMilliseconds = 25 with get, set
    
    member val IndexVersion = IndexVersion.Lucene_4_9 with get, set
    member val IdFieldPostingsFormat = FieldPostingsFormat.Bloom_4_1 with get, set
    member val DefaultIndexPostingsFormat = FieldPostingsFormat.Lucene_4_1 with get, set
    member val DefaultCodec = Codec.Lucene_4_9 with get, set
    member val EnableVersioning = false with get, set
    member val DefaultFieldSimilarity = FieldSimilarity.TFIDF with get, set
    member val IdFieldDocvaluesFormat = FieldDocValuesFormat.Lucene_4_9 with get, set
    member val DefaultDocvaluesFormat = FieldDocValuesFormat.Lucene_4_9 with get, set

[<ToString>]
type FieldProperties() = 
    inherit ValidatableObjectBase<FieldProperties>()
    
    [<DefaultValue(true)>]
    member val Analyze = true with get, set
    
    [<DefaultValue(true)>]
    member val Index = true with get, set
    
    [<DefaultValue(true)>]
    member val Store = true with get, set
    
    [<DefaultValue(StandardAnalyzer)>]
    member val IndexAnalyzer = StandardAnalyzer with get, set
    
    [<DefaultValue(StandardAnalyzer)>]
    member val SearchAnalyzer = StandardAnalyzer with get, set
    
    member val FieldType = FieldType.Text with get, set
    member val PostingsFormat = FieldPostingsFormat.Lucene_4_1 with get, set
    member val Similarity = FieldSimilarity.TFIDF with get, set
    member val IndexOptions = FieldIndexOptions.DocsAndFreqsAndPositions with get, set
    member val TermVector = FieldTermVector.DoNotStoreTermVector with get, set
    member val OmitNorms = true with get, set
    member val ScriptName = "" with get, set
    member val DocValuesFormat = FieldDocValuesFormat.Lucene_4_9 with get, set
    override this.Validate(context) = 
        seq { 
            if (this.FieldType = FieldType.Text || this.FieldType = FieldType.Highlight 
                || this.FieldType = FieldType.Custom) 
               && (String.IsNullOrWhiteSpace(this.SearchAnalyzer) || String.IsNullOrWhiteSpace(this.IndexAnalyzer)) then 
                yield new ValidationResult("SearchAnalyzer and IndexAnalyzer are mandatory for Text, Highlight and Custom field types.")
        }

[<ToString>]
type Job() = 
    inherit ValidatableObjectBase<Job>()
    member val JobId = "" with get, set
    member val TotalItems = 0 with get, set
    member val ProcessedItems = 0 with get, set
    member val FailedItems = 0 with get, set
    member val Status = JobStatus.Initializing with get, set
    member val Message = "" with get, set

// ----------------------------------------------------------------------------
//	Analyzer related
// ----------------------------------------------------------------------------
[<ToString>]
type TokenFilter(filterName : string) = 
    inherit ValidatableObjectBase<TokenFilter>()
    
    [<Required; PropertyName>]
    member val FilterName = filterName with get, set
    
    [<MinimumItems(1)>]
    member val Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    new() = TokenFilter(Unchecked.defaultof<string>)

[<ToString>]
type Tokenizer(tokenizerName : string) = 
    inherit ValidatableObjectBase<Tokenizer>()
    
    [<Required; PropertyName>]
    member val TokenizerName = tokenizerName with get, set
    
    member val Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    new() = Tokenizer(Unchecked.defaultof<_>)

[<ToString>]
type AnalyzerProperties() = 
    inherit ValidatableObjectBase<AnalyzerProperties>()
    
    [<Required; ValidateComplex>]
    member val Tokenizer = Unchecked.defaultof<Tokenizer> with get, set
    
    [<Required; MinimumItems(1, ErrorMessage = "Analyzer requires at least 1 Filter to be defined.")>]
    member val Filters = new List<TokenFilter>() with get, set
    
    override this.Validate(context) = seq { yield Helpers.ValidateCollection<TokenFilter>(this.Filters) }

// ----------------------------------------------------------------------------
//	Scripting related
// ----------------------------------------------------------------------------
type ScriptProperties(source : string, scriptType : ScriptType) = 
    inherit ValidatableObjectBase<ScriptProperties>()
    
    [<Required>]
    member val Source = source with get, set
    
    member val ScriptType = ScriptType.ComputedField with get, set
    new() = ScriptProperties(Unchecked.defaultof<string>, ScriptType.ComputedField)

// ----------------------------------------------------------------------------
//	Search related
// ----------------------------------------------------------------------------
type MissingValueOption = 
    | ThrowError = 1
    | Default = 2
    | Ignore = 3

type HighlightOption(fields : List<string>) = 
    inherit ValidatableObjectBase<HighlightOption>()
    member val FragmentsToReturn = 2 with get, set
    member val HighlightedFields = fields with get, set
    member val PostTag = "</B>" with get, set
    member val PreTag = "</B>" with get, set
    new() = HighlightOption(Unchecked.defaultof<List<string>>)

type SearchQuery(index : string, query : string) = 
    inherit ValidatableObjectBase<SearchQuery>()
    member val Columns = new List<string>() with get, set
    
    [<DefaultValue(10)>]
    member val Count = 10 with get, set
    
    member val Highlights = Unchecked.defaultof<HighlightOption> with get, set
    
    [<Required>]
    member val IndexName = index with get, set
    
    [<DefaultValue("score")>]
    member val OrderBy = "score" with get, set
    
    member val Skip = 0 with get, set
    
    [<Required>]
    member val QueryString = query with get, set
    
    member val MissingValueConfiguration = new Dictionary<string, MissingValueOption>(StringComparer.OrdinalIgnoreCase) with get, set
    member val GlobalMissingValue = MissingValueOption.Default with get, set
    
    [<DefaultValue(false)>]
    member val ReturnFlatResult = false with get, set
    
    [<DefaultValue(true)>]
    member val ReturnScore = true with get, set
    
    member val SearchProfile = Unchecked.defaultof<string> with get, set
    member val SearchProfileSelector = Unchecked.defaultof<string> with get, set
    new() = SearchQuery(Unchecked.defaultof<_>, Unchecked.defaultof<_>)

// ----------------------------------------------------------------------------
//	Index & Document related
// ----------------------------------------------------------------------------
type Document() = 
    inherit ValidatableObjectBase<ScriptProperties>()
    member val Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    member val Highlights = new List<string>() with get, set
    member val Score = 0.0 with get, set
    
    [<Required>]
    member val Id = Unchecked.defaultof<string> with get, set
    
    member val LastModified = Unchecked.defaultof<Int64> with get, set
    [<Required>]
    member val Index = Unchecked.defaultof<string> with get, set

type Index() = 
    inherit ValidatableObjectBase<Index>()
    
    [<PropertyName; Required(AllowEmptyStrings = false)>]
    member val IndexName = Unchecked.defaultof<string> with get, set
    
    [<ValidKeys>]
    member val Analyzers = new Dictionary<string, AnalyzerProperties>(StringComparer.OrdinalIgnoreCase) with get, set
    
    [<ValidKeys>]
    member val Fields = new Dictionary<string, FieldProperties>(StringComparer.OrdinalIgnoreCase) with get, set
    
    [<ValidKeys>]
    member val Scripts = new Dictionary<string, ScriptProperties>(StringComparer.OrdinalIgnoreCase) with get, set
    
    [<ValidKeys>]
    member val SearchProfiles = new Dictionary<string, SearchQuery>(StringComparer.OrdinalIgnoreCase) with get, set
    
    member val ShardConfiguration = new ShardConfiguration() with get, set
    member val IndexConfiguration = new IndexConfiguration() with get, set
    member val Online = false with get, set
    override this.Validate(context) = 
        seq { 
            yield Helpers.ValidateCollection<AnalyzerProperties>(this.Analyzers.Values)
            yield Helpers.ValidateCollection<FieldProperties>(this.Fields.Values)
            yield Helpers.ValidateCollection<ScriptProperties>(this.Scripts.Values)
            yield Helpers.ValidateCollection<SearchQuery>(this.SearchProfiles.Values)
            for field in this.Fields do
                // Check if the specified script exists
                if String.IsNullOrWhiteSpace(field.Value.ScriptName) = false then 
                    match this.Scripts.TryGetValue(field.Value.ScriptName) with
                    | (true, _) -> ()
                    | _ -> 
                        yield new ValidationResult(Errors.SCRIPT_NOT_FOUND 
                                                   |> AppendKv("ScriptName", field.Value.ScriptName))
        }

type SearchResults() = 
    member val Documents = new List<Document>() with get, set
    member val RecordsReturned = 0 with get, set
    member val TotalAvailable = 0 with get, set

type FilterList(words : List<string>) = 
    inherit ValidatableObjectBase<FilterList>()
    
    [<MinimumItems(1)>]
    member val Words = words with get, set
    
    new() = FilterList(Unchecked.defaultof<List<string>>)

type MapList(words : Dictionary<string, List<string>>) = 
    inherit ValidatableObjectBase<MapList>()
    
    [<MinimumItems(1)>]
    member val Words = words with get, set
    
    new() = MapList(new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase))

type IndexStatusResponse(state) = 
    member val Status = state

type ImportRequest() = 
    inherit ValidatableObjectBase<ImportRequest>()
    member val Id = "" with get, set
    member val Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    member val ForceCreate = false with get, set
    member val JobId = "" with get, set

type ImportResponse() = 
    member val JobId = "" with get, set
    member val Message = "" with get, set

// ----------------------------------------------------------------------------
//	Server Settings
// ----------------------------------------------------------------------------
type ServerSettings() = 
    member val HttpPort = 9800 with get, set
    member val ThriftPort = 9900 with get, set
    member val DataFolder = "./data" with get, set
    member val PluginFolder = "./plugins" with get, set
    member val ConfFolder = "./conf" with get, set
    member val NodeName = "FlexNode" with get, set
    member val NodeRole = 1 with get, set
    member val Logger = "Gibraltar" with get, set
