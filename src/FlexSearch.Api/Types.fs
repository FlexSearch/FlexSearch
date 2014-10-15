namespace FlexSearch.Api

open FlexSearch.Api.Validation
open NullGuard
open System
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.DataAnnotations
open System.Linq

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
    
    member val IndexVersion = IndexVersion.Lucene_4_10_1 with get, set
    member val IdFieldPostingsFormat = FieldPostingsFormat.Bloom_4_1 with get, set
    
    /// <summary>
    /// This will be computed at run time based on the index version
    /// </summary>
    [<Display(AutoGenerateField = false)>]
    member val DefaultIndexPostingsFormat = Unchecked.defaultof<FieldPostingsFormat> with get, set
    
    member val DefaultFieldSimilarity = FieldSimilarity.TFIDF with get, set

[<ToString>]
type Field(fieldName : string, fieldType : FieldType) = 
    inherit ValidatableObjectBase<Field>()
    
    [<Required; PropertyName>]
    member val FieldName = fieldName with get, set
    
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
    
    member val FieldType = fieldType with get, set
    member val PostingsFormat = FieldPostingsFormat.Lucene_4_1 with get, set
    member val Similarity = FieldSimilarity.TFIDF with get, set
    member val IndexOptions = FieldIndexOptions.DocsAndFreqsAndPositions with get, set
    member val TermVector = FieldTermVector.DoNotStoreTermVector with get, set
    member val OmitNorms = true with get, set
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
type Analyzer() = 
    inherit ValidatableObjectBase<Analyzer>()
    
    [<Required; ValidateComplex>]
    member val Tokenizer = Unchecked.defaultof<Tokenizer> with get, set
    
    [<Required; MinimumItems(1, ErrorMessage = "Analyzer requires at least 1 Filter to be defined.")>]
    member val Filters = new List<TokenFilter>() with get, set
    
    override this.Validate(context) = seq { yield Helpers.ValidateCollection<TokenFilter>(this.Filters) }

// ----------------------------------------------------------------------------
//	Scripting related
// ----------------------------------------------------------------------------
type Script(scriptName : string, source : string, scriptType : ScriptType) = 
    inherit ValidatableObjectBase<Script>()
    
    [<Required; PropertyName>]
    member val ScriptName = scriptName with get, set
    
    [<Required>]
    member val Source = source with get, set
    
    member val ScriptType = ScriptType.ComputedField with get, set
    new() = Script(Unchecked.defaultof<string>, Unchecked.defaultof<string>, ScriptType.ComputedField)

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
    member val QueryName = Unchecked.defaultof<string> with get, set
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
type ResultDocument(indexName : string, id : string) = 
    inherit ValidatableObjectBase<Script>()
    member val Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    member val Highlights = new List<string>() with get, set
    member val Score = 0.0 with get, set
    
    [<Required>]
    member val Id = id with get, set
    
    member val TimeStamp = Unchecked.defaultof<Int64> with get, set
    
    [<Required>]
    member val IndexName = indexName with get, set
    
    new() = ResultDocument(Unchecked.defaultof<string>, Unchecked.defaultof<string>)

type FlexDocument(indexName : string, id : string) = 
    inherit ValidatableObjectBase<Script>()
    member val Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    [<Required>]
    member val Id = id with get, set
    
    member val TimeStamp = Unchecked.defaultof<Int64> with get, set
    
    [<Required>]
    member val IndexName = indexName with get, set
    
    new() = FlexDocument(Unchecked.defaultof<string>, Unchecked.defaultof<string>)

[<ToString>]
type Index() = 
    inherit ValidatableObjectBase<Index>()
    
    [<PropertyName; Required(AllowEmptyStrings = false)>]
    member val IndexName = Unchecked.defaultof<string> with get, set
    
    [<ValidKeys>]
    member val Analyzers = new Dictionary<string, Analyzer>(StringComparer.OrdinalIgnoreCase) with get, set
    
    member val Fields = new List<Field>() with get, set
    member val Scripts = new List<Script>() with get, set
    member val SearchProfiles = new List<SearchQuery>() with get, set
    member val ShardConfiguration = new ShardConfiguration() with get, set
    member val IndexConfiguration = new IndexConfiguration() with get, set
    member val Online = false with get, set
    override this.Validate(context) = 
        seq { 
            yield Helpers.ValidateCollection<Analyzer>(this.Analyzers.Values)
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

type SearchResults() = 
    member val Documents = new List<ResultDocument>() with get, set
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

type ImportRequest() = 
    inherit ValidatableObjectBase<ImportRequest>()
    member val Id = "" with get, set
    member val Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    member val ForceCreate = false with get, set
    member val JobId = "" with get, set

type ImportResponse() = 
    member val JobId = "" with get, set
    member val Message = "" with get, set
