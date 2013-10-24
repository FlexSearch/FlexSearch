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
open System.Runtime.Serialization

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

 
    type IndexFieldProperties() =
        member val Analyze = true with get, set
        member val Index = true with get, set
        member val store = true with get, set
        member val IndexAnalyzer = "standardanalyzer" with get, set
        member val SearchAnalyzer = "standardanalyzer" with get, set
        member val FieldType = FieldType.Text with get, set
        member val FieldPostingsFormat = FieldPostingsFormat.Lucene41PostingsFormat with get, set
        member val FieldIndexOptions = FieldIndexOptions.DocsAndFreqsAndPositions with get, set
        member val FieldTermVector = FieldTermVector.StoreTermVectorsWithPositions with get, set
        member val OmitNorms = true with get, set

