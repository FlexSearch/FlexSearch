namespace FlexSearch.Core

/// Similarity defines the components of Lucene scoring. Similarity 
/// determines how Lucene weights terms, and Lucene interacts with 
/// Similarity at both index-time and query-time.
type FieldSimilarity = 
    | Undefined = 0
    /// BM25 Similarity defines the components of Lucene scoring.
    | BM25 = 1
    /// TFIDF Similarity defines the components of Lucene scoring. 
    /// This is the default Lucene similarity.
    | TFIDF = 2

type DirectoryType = 
    | FileSystem = 1
    | MemoryMapped = 2
    | Ram = 3

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