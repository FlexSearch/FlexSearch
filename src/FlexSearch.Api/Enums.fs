namespace FlexSearch.Api

open System.Runtime.Serialization

type FieldSimilarity = 
    | BM25 = 1
    | TFIDF = 2

type FieldPostingsFormat = 
    | Direct = 1
    | Memory = 2
    | Bloom_4_1 = 3
    | Pulsing_4_1 = 4
    | Lucene_4_1 = 5

type FieldDocValuesFormat = 
    | Direct = 1
    | Memory = 2
    | Lucene_4_9 = 3
    | LUCENE_4_10 = 4

type DirectoryType = 
    | FileSystem = 1
    | MemoryMapped = 2
    | Ram = 3

type FieldTermVector = 
    | DoNotStoreTermVector = 1
    | StoreTermVector = 2
    | StoreTermVectorsWithPositions = 3
    | StoreTermVectorsWithPositionsandOffsets = 4

type FieldIndexOptions = 
    | DocsOnly = 1
    | DocsAndFreqs = 2
    | DocsAndFreqsAndPositions = 3
    | DocsAndFreqsAndPositionsAndOffsets = 4

type FieldType = 
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
    | Long = 11

type ShardAllocationStrategy = 
    | Automatic = 1
    | Manual = 2

type IndexVersion = 
    | Lucene_4_9 = 1
    | LUCENE_4_10 = 2

type Codec = 
    | Lucene_4_9 = 1
    | LUCENE_4_10 = 2

type ScriptType = 
    | SearchProfileSelector = 1
    | CustomScoring = 2
    | ComputedField = 3

type IndexState = 
    | Opening = 1
    | Online = 2
    | Offline = 3
    | Closing = 4

type JobStatus = 
    | Initializing = 1
    | Initialized = 2
    | InProgress = 3
    | Completed = 4
    | CompletedWithErrors = 5
