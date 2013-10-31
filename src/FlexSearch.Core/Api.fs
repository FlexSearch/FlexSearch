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
namespace FlexSearch
open System.Runtime.Serialization
open System.Collections.Generic

module Api =
    // Represents Lucene's similarity models
    type [<DataContract(Namespace = "")>] FieldSimilarity =
        | [<EnumMember>] BM25 = 1
        | [<EnumMember>] TDF = 2


    // Lucene's field postings format
    type [<DataContract(Namespace = "")>] FieldPostingsFormat = 
        | [<EnumMember>] Direct = 1
        | [<EnumMember>] Memory = 2
        | [<EnumMember>] Bloom = 3
        | [<EnumMember>] Pulsing = 4
        | [<EnumMember>] Lucene41PostingsFormat = 5

    type [<DataContract(Namespace = "")>] DirectoryType =
        | [<EnumMember>] FileSystem = 1
        | [<EnumMember>] MemoryMapped = 2
        | [<EnumMember>] Ram = 3

    type [<DataContract(Namespace = "")>] FieldTermVector =
        | [<EnumMember>] DoNotStoreTermVector = 1
        | [<EnumMember>] StoreTermVector = 2
        | [<EnumMember>] StoreTermVectorsWithPositions = 3
        | [<EnumMember>] StoreTermVectorsWithPositionsandOffsets = 3


    type [<DataContract(Namespace = "")>] FieldIndexOptions =
        /// Only documents are indexed: term frequencies and positions are omitted. 
        /// Phrase and other positional queries on the field will throw an exception, 
        /// and scoring will behave as if any term in the document appears only once.
        | [<EnumMember>] DocsOnly = 1

        /// Only documents and term frequencies are indexed: positions are omitted. This enables normal scoring, 
        /// except Phrase and other positional queries will throw an exception.
        | [<EnumMember>] DocsAndFreqs = 2

        /// Indexes documents, frequencies and positions. This is a typical default for full-text 
        /// search: full scoring is enabled and positional queries are supported.
        | [<EnumMember>] DocsAndFreqsAndPositions = 3
        
        /// Indexes documents, frequencies, positions and offsets. Character offsets are encoded alongside the positions.
        | [<EnumMember>] SDocsAndFreqsAndPositionsAndOffsets = 3


    type [<DataContract(Namespace = "")>] FieldType =
        | Int = 1
        | Double = 2
        | ExactText = 3
        | Text = 4
        | Highlight = 5
        | Bool = 6
        | Date = 7
        | DateTime = 8
        | Custom = 9
        | Stored = 10


    type [<DataContract(Namespace = "")>] IndexConfiguration()  =
        [<DataMember(Order = 1)>] member val CommitTimeSec = 60 with get, set
        [<DataMember(Order = 2)>] member val DirectoryType = DirectoryType.MemoryMapped with get, set
        [<DataMember(Order = 3)>] member val DefaultWriteLockTimeout =  1000 with get, set
        
        /// Determines the amount of RAM that may be used for buffering added documents and deletions before they are flushed to the Directory.
        [<DataMember(Order = 4)>] member val RamBufferSizeMb = 500 with get, set

        [<DataMember(Order = 5)>] member val RefreshTimeMilliSec = 25 with get, set
        [<DataMember(Order = 6)>] member val Shards = 1 with get, set

 
    type [<DataContract(Namespace = "")>] IndexFieldProperties() =
        [<DataMember(Order = 1)>] member val Analyze = true with get, set
        [<DataMember(Order = 2)>] member val Index = true with get, set
        [<DataMember(Order = 3)>] member val Store = true with get, set
        [<DataMember(Order = 4)>] member val IndexAnalyzer = "standardanalyzer" with get, set
        [<DataMember(Order = 5)>] member val SearchAnalyzer = "standardanalyzer" with get, set
        [<DataMember(Order = 6)>] member val FieldType = FieldType.Text with get, set
        [<DataMember(Order = 7)>] member val FieldPostingsFormat = FieldPostingsFormat.Lucene41PostingsFormat with get, set
        [<DataMember(Order = 8)>] member val FieldIndexOptions = FieldIndexOptions.DocsAndFreqsAndPositions with get, set
        [<DataMember(Order = 9)>] member val FieldTermVector = FieldTermVector.StoreTermVectorsWithPositions with get, set
        [<DataMember(Order = 10)>] member val OmitNorms = true with get, set
        [<DataMember(Order = 11)>] member val ScriptName = "" with get, set


    [<CollectionDataContract(Namespace = "", ItemName = "Field", KeyName = "FieldName", ValueName = "Properties")>]
    type FieldDictionary() =
        inherit Dictionary<string, IndexFieldProperties>()


    [<CollectionDataContract(Namespace = "", ItemName = "KeyValuePair", KeyName = "Key", ValueName = "Value")>]
    type KeyValuePairs() =
        inherit Dictionary<string, string>()


    type [<DataContract(Namespace = "")>] TokenFilter() =
        [<DataMember(Order = 1)>] member val FilterName = "standardfilter" with get, set
        [<DataMember(Order = 2)>] member val Parameters = new KeyValuePairs() with get, set


    type [<DataContract(Namespace = "")>] Tokenizer() =
        [<DataMember(Order = 1)>] member val TokenizerName = "standardtokenizer" with get, set
        [<DataMember(Order = 2)>] member val Parameters = new KeyValuePairs() with get, set


    type [<DataContract(Namespace = "")>] AnalyzerProperties() = 
        [<DataMember(Order = 1)>] member val Filters = new List<TokenFilter>() with get, set
        [<DataMember(Order = 2)>] member val Tokenizer = new Tokenizer() with get, set


    [<CollectionDataContract(Namespace = "", ItemName = "Analyzer", KeyName = "AnalyzerName", ValueName = "Properties")>]
    type AnalyzerDictionary() =
        inherit Dictionary<string, AnalyzerProperties>()

    type ScriptOption =
        | SingleLine = 1
        | MultiLine = 2
        | FileBased = 3

    type ScriptType =
        | SearchProfileSelector = 1
        | CustomScoring = 2
        | ComputedField = 3


    type ScriptProperties() =
         member val ScriptOption = ScriptOption.SingleLine with get, set
         member val ScriptSource = "" with get, set
         member val ScriptType = ScriptType.ComputedField with get, set
    

    [<CollectionDataContract(Namespace = "", ItemName = "Script", KeyName = "ScriptName", ValueName = "Properties")>]
    type ScriptDictionary() =
        inherit Dictionary<string, ScriptProperties>()
    

    [<CollectionDataContract(Namespace = "", ItemName = "Value")>]
    type StringList() =
        inherit List<string>()

    type FilterType =
        | And = 1
        | Or = 2

    type MissingValueOption =
        | ThrowError = 1
        | Default = 2
        | Ignore = 3


    type SearchCondition() =
        member val Boost = 1 with get, set 
        member val FieldName = "" with get, set 
        member val MissingValueOption = MissingValueOption.ThrowError with get, set 
        member val Operator = "" with get, set 
        member val Parameters = new KeyValuePairs() with get, set 
        member val Values = new StringList() with get, set 


    type SearchFilter() =
        member val FilterType = FilterType.And with get, set 
        member val Conditions = new List<SearchCondition>() with get, set 
        member val SubFilters = new List<SearchFilter>() with get, set 
        member val ConstantScore = 0 with get, set 
    
    type HighlightOption() =
        member val FragmentsToReturn = 2 with get, set 
        member val HighlightedFields = new StringList() with get, set 
        member val PostTag = "</B>" with get, set 
        member val PreTag = "</B>" with get, set 


    type SearchQuery() =
        member val Columns = new StringList() with get, set 
        member val Count = 10 with get, set 
        member val Highlight = Unchecked.defaultof<HighlightOption> with get, set 
        member val IndexName = "" with get, set 
        member val OrderBy = "" with get, set 
        member val Skip = 0 with get, set 
        member val Query = new SearchFilter() with get, set 


    type SearchProfileQuery() =
        member val Fields = new KeyValuePairs() with get, set 
        member val SearchProfileSelector = "" with get, set 
        member val IndexName = "" with get, set 
        member val SearchProfileName = "" with get, set 


    [<CollectionDataContract(Namespace = "", ItemName = "SearchProfile", KeyName = "ProfileName", ValueName = "Properties")>]
    type SearchProfileDictionary() =
        inherit Dictionary<string, SearchQuery>()
    
    type Document() =
        member val Fields = new KeyValuePairs() with get, set 
        member val Highlights = new StringList() with get, set
        member val Id = "" with get, set 
        member val LastModified = "" with get, set
        member val Version = 1 with get, set
        member val Index = "" with get, set
        member val Score = 0.0 with get, set 

    type SearchResults() =
        member val Documents = new List<Document>() with get, set 
        member val RecordsReturned = 0 with get, set 
        member val TotalAvailable = 0 with get, set

    type Index() =
        member val Analyzers = new AnalyzerDictionary() with get, set
        member val Configuration = new IndexConfiguration() with get, set
        member val Fields = new FieldDictionary() with get, set
        member val IndexName = "" with get, set
        member val Online = true with get, set
        member val Scripts = new ScriptDictionary() with get, set
        member val SearchProfiles = new SearchProfileDictionary() with get, set