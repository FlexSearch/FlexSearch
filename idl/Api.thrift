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

/**
 * Available types in Thrift
 *
 *  bool        Boolean, one byte
 *  byte        Signed byte
 *  i16         Signed 16-bit integer
 *  i32         Signed 32-bit integer
 *  i64         Signed 64-bit integer
 *  double      64-bit floating point value
 *  string      String
 *  binary      Blob (byte array)
 *  map<t1,t2>  Map from one type to another
 *  list<t1>    Ordered list of one type
 *  set<t1>     Set of unique elements of one type
 *
 */
 
namespace csharp FlexSearch.Api
namespace java org.FlexSearch.Api

// ----------------------------------------------------------------------------
//	Enums
// ----------------------------------------------------------------------------

// Node role
	enum NodeRole {
		Master = 1
		Slave = 2
	}


	enum FieldSimilarity {
		BM25 = 1
		TFIDF = 2
	}

/*
<Field Postings Format
^^^^^^^^^^^^^^^^^^^^^^^^

Encodes/decodes terms, postings, and proximity data.

**Memory FieldPostingsFormat**
Postings and DocValues formats that are read entirely into memory.

**Bloom FieldPostingsFormat**
A PostingsFormat useful for low doc-frequency fields such as primary keys.

**Pulsing FieldPostingsFormat**
Pulsing Codec: in-lines low frequency terms' postings into terms dictionary.

.. code-block:: c

*/
	enum FieldPostingsFormat {
		Direct = 1
		Memory = 2
		Bloom = 3 
		Pulsing = 4 
		Lucene_4_1 = 5
	}
//>

/*
<Directory Type
^^^^^^^^^^^^^^^^^

**Directory**
A Directory is a flat list of files. Files may be written once, when they are created. Once a file is created it may only be opened for read, or deleted. Random access is permitted both when reading and writing.

**Ram Directory**
A memory-resident Directory implementation. This is not intended to work with huge indexes. Everything beyond several hundred megabytes will waste resources (GC cycles), because it uses an internal buffer size of 1024 bytes, producing millions of byte[1024] arrays. This class is optimized for small memory-resident indexes. It also has bad concurrency on multithreaded environments.
It is recommended to materialize large indexes on disk and use MMapDirectory, which is a high-performance directory implementation working directly on the file system cache of the operating system.

**MemoryMapped Directory**
File-based Directory implementation that uses memory map for reading, and FSDirectory.FSIndexOutput for writing.
NOTE: memory mapping uses up a portion of the virtual memory address space in your process equal to the size of the file being mapped. Before using this class, be sure your have plenty of virtual address space, e.g. by using a 64 bit JRE, or a 32 bit JRE with indexes that are guaranteed to fit within the address space. On 32 bit platforms also consult MMapDirectory(File, LockFactory, int) if you have problems with mmap failing because of fragmented address space. If you get an OutOfMemoryException, it is recommended to reduce the chunk size, until it works.

Due to this bug in Sun's JRE, MMapDirectory's IndexInput.close() is unable to close the underlying OS file handle. Only when GC finally collects the underlying objects, which could be quite some time later, will the file handle be closed.

This will consume additional transient disk usage: on Windows, attempts to delete or overwrite the files will result in an exception; on other platforms, which typically have a "delete on last close" semantics, while such operations will succeed, the bytes are still consuming space on disk. For many applications this limitation is not a problem (e.g. if you have plenty of disk space, and you don't rely on overwriting files on Windows) but it's still an important limitation to be aware of.

**FileSystem Directory**
FileSystem Directory is a straightforward implementation using java.io.RandomAccessFile. However, it has poor concurrent performance (multiple threads will bottleneck) as it synchronizes when multiple threads read from the same file.

.. code-block:: c

*/
	enum DirectoryType {
		FileSystem = 1
		MemoryMapped = 2
		Ram = 3
	}
//>

/*
<Field Term Vector
^^^^^^^^^^^^^^^^^^^^

These options instruct FlexSearch to maintain full term vectors for each document, optionally 
including the position and offset information for each term occurrence in those vectors. These 
can be used to accelerate highlighting and other ancillary functionality, but impose a substantial 
cost in terms of index size. These can only be configured for custom field type.

**DoNotStoreTermVector**
Do not store term vectors.

**StoreTermVector**
Store the term vectors of each document. A term vector is a list of the document's terms and their number of occurrences in that document.

**StoreTermVectorsWithPositions**
Store the term vector < token position information

**StoreTermVectorsWithPositionsandOffsets**
Store the term vector + Token position and offset information

.. code-block:: c

*/
	enum FieldTermVector {
		DoNotStoreTermVector = 1
		StoreTermVector = 2
		StoreTermVectorsWithPositions = 3
		StoreTermVectorsWithPositionsandOffsets = 4
	}
//>

/*
<FieldIndexOptions
^^^^^^^^^^^^^^^^^^^

**DocsOnly**
Only documents are indexed: term frequencies and positions are omitted. Phrase and other positional queries on the field will throw an exception, and scoring will behave as if any term in the document appears only once.

**DocsAndFreqs**
Only documents and term frequencies are indexed: positions are omitted. This enables normal scoring, except Phrase and other positional queries will throw an exception.

**DocsAndFreqsAndPositions**
Indexes documents, frequencies and positions. This is a typical default for full-text search: full scoring is enabled and positional queries are supported.

**DocsAndFreqsAndPositionsAndOffsets**
Indexes documents, frequencies, positions and offsets. Character offsets are encoded alongside the positions.

.. code-block:: c

*/
	enum FieldIndexOptions {
		DocsOnly = 1
		DocsAndFreqs = 2
		DocsAndFreqsAndPositions = 3
		DocsAndFreqsAndPositionsAndOffsets = 4
	}
//>

/*
<Field Type
^^^^^^^^^^^^^

The field type defines how FlexSearch should interpret data in a field and how the field can be queried. There are many field types included with FlexSearch by default, and custom types can also be defined.

The below table list the various field types supported by FlexSearch.

.. tabularcolumns:: |p{2cm}|J|
.. rst-class:: ui celled table
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Field Type   | Description                                                                                                                                                                                                                                        |
+==============+====================================================================================================================================================================================================================================================+
| Int          | Integer                                                                                                                                                                                                                                            |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Double       | Double                                                                                                                                                                                                                                             |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| ExactText    | Field to store keywords. The entire input will be treated as a single word. This is useful for fields like customerid, referenceid etc. These fields only support complete text matching while searching and no partial word match is available.   |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Text         | General purpose field to store normal textual data                                                                                                                                                                                                 |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Highlight    | Similar to Text field but supports highlighting of search results                                                                                                                                                                                  |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Bool         | Boolean                                                                                                                                                                                                                                            |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Date         | Fixed format date field (Supported format: YYYYmmdd)                                                                                                                                                                                               |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| DateTime     | Fixed format datetime field (Supported format: YYYYMMDDhhmmss)                                                                                                                                                                                     |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Custom       | Custom field type which gives more granular control over the field configuration                                                                                                                                                                   |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Stored       | Non-indexed field. Only used for retrieving stored text. Searching is not possible over these fields.                                                                                                                                              |
+--------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+

*/
/*
The below table lists the various parameters which can be configured for each field type.

.. rst-class:: ui celled table
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| Field Type   | Search Analyzer   | Index Analyzer   | Store   | Index   | Analyze   | Term Vector   |
+==============+===================+==================+=========+=========+===========+===============+
| Int          | No                | No               | Yes     | No      | No        | No            |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| Double       | No                | No               | Yes     | No      | No        | No            |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| ExactText    | No                | No               | Yes     | No      | No        | No            |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| Text         | Yes               | Yes              | Yes     | No      | No        | No            |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| Highlight    | Yes               | Yes              | Yes     | No      | No        | No            |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| Bool         | No                | No               | Yes     | No      | No        | No            |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| Date         | No                | No               | Yes     | No      | No        | No            |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| DateTime     | No                | No               | Yes     | No      | No        | No            |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| Custom       | Yes               | Yes              | Yes     | Yes     | Yes       | Yes           |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+
| Stored       | No                | No               | No      | No      | No        | No            |
+--------------+-------------------+------------------+---------+---------+-----------+---------------+

.. note:: 
    By default Text, Highlight and Custom use Standard Analyzer for searching and indexing.

.. note:: 
    Configuring any unsupported combination for a field type will be ignored and will result in unexpected behaviour.

.. code-block:: c

*/
	enum FieldType {
		Int = 1
		Double = 2
		ExactText = 3
		Text = 4
		Highlight = 5
		Bool = 6
		Date = 7
		DateTime = 8
		Custom = 9
		Stored = 10
		Long = 11
	}
//>

	enum ShardAllocationStrategy {
		Automatic = 1
		Manual = 2
	}

/*
IndexVersion
^^^^^^^^^^^^^^^^

Version of the Lucene index used behind the scene. 

.. code-block:: c

*/
	enum IndexVersion {
		Lucene_4_9 = 1
	}
//>


enum Codec {
		Lucene_4_9 = 1
	}

/*
<Script Type
^^^^^^^^^^^^^

.. code-block:: c

*/
	enum ScriptType {
		SearchProfileSelector = 1
		CustomScoring = 2
		ComputedField = 3
	}
//>

/*
<Index State
^^^^^^^^^^^^

.. code-block:: c

*/
	enum IndexState {
		Opening = 1
		Online = 2
		Offline = 3
		Closing = 4
	}
//>

/*
<Job Status
^^^^^^^^^^^

.. code-block:: c

*/
	enum JobStatus {
		Initializing = 1
		Initialized = 2
		InProgress = 3
		Completed = 4
		CompletedWithErrors = 5
	}
//>

// ----------------------------------------------------------------------------
//	Structs
// ----------------------------------------------------------------------------

/*
<Shard Configuration
^^^^^^^^^^^^^^^^^^^^^^

.. code-block:: c

*/
	struct ShardConfiguration {
		1:	optional i32 ShardCount = 1
	}
//>

/*
<Index Configuration
^^^^^^^^^^^^^^^^^^^^^^

**CommitTimeSec**

**DirectoryType**
Refer to directory type.

**DefaultWriteLockTimeout**

**RamBufferSizeMb**
Determines the amount of RAM that may be used for buffering added documents and deletions before they are flushed to the Directory.

**RefreshTimeMilliSec**

**IndexVersion**
Refer to IndexVersion.

.. code-block:: c

*/
	struct IndexConfiguration {
		1:	optional i32 CommitTimeSec = 60
		2:	optional DirectoryType DirectoryType = 2
		3:	optional i32 DefaultWriteLockTimeout =  1000
		4:	optional i32 RamBufferSizeMb = 100
		5:	optional i32 RefreshTimeMilliSec = 25
		6:	optional IndexVersion IndexVersion = IndexVersion.Lucene_4_9
		7:	optional FieldPostingsFormat IdFieldPostingsFormat = FieldPostingsFormat.Bloom
		8:	optional FieldPostingsFormat DefaultIndexPostingsFormat = FieldPostingsFormat.Lucene_4_1
		9:	optional Codec DefaultCodec = Codec.Lucene_4_9
		10:	optional bool EnableVersioning = false
	}
//>

/*
<Field Properties
^^^^^^^^^^^^^^^^^^^

.. rst-class:: ui celled table
.. list-table:: Field Properties
   :header-rows: 1
   :widths: 10 40
   :stub-columns: 1

   *  -  Property Name
      -  Description
   *  -  ``FieldName``
      -  The name of the field. This should be lower case and should only contain alphabetical characters.
   *  -  ``Analyze``
      -  Signifies if the field should be analyzed using an analyzer.
   *  -  ``Index``
      -  Signifies if a field should be indexed. A field can only be stored without indexing.
   *  -  ``Store``
      -  Signifies if a field should be stored so that it can retrieved while searching.
   *  -  ``FieldTermVector``
      -  Advance property used for highlighting.
   *  -  ``FieldType``
      -  The type of field
   *  -  ``IndexAnalyzer``
      -  Analyzer to be used while indexing 
   *  -  ``SearchAnalyzer``
      -  Analyzer to be used while searching
   *  -  ``ScriptName``
      -  Fields can get their content dynamically through scripts. This is the name of the script to be used for getting field data at index time.

.. code-block:: c
 
*/
	struct FieldProperties {
		1:	optional bool Analyze = true
		2:	optional bool Index = true
		3:	optional bool Store = true
		4:	optional string IndexAnalyzer = "standardanalyzer"
		5:	optional string SearchAnalyzer = "standardanalyzer"
		6:	optional FieldType FieldType = 4
		7:	optional FieldPostingsFormat PostingsFormat = 5
		8:	optional FieldSimilarity Similarity = 2
		9:	optional FieldIndexOptions IndexOptions = 3
		10:	optional FieldTermVector TermVector = 3
		11:	optional bool OmitNorms = true
		12:	optional string ScriptName = ""
	}
//>

struct Job {
	1:	required string JobId
	2:	optional i32 TotalItems
	3:	optional i32 ProcessedItems
	4:	optional i32 FailedItems
	5:	required JobStatus Status = 1
	6:	optional string Message
}

// ----------------------------------------------------------------------------
//	Analyzer related
// ----------------------------------------------------------------------------
/*
<Token Filter
^^^^^^^^^^^^^^^^

Like tokenizers, filters consume input and produce a stream of tokens. The job of a filter is usually easier than that of a tokenizer since in most cases a filter looks 
at each token in the stream sequentially and decides whether to pass it along, replace it or discard it.

A filter may also do more complex analysis by looking ahead to consider multiple tokens at once, although this is less common. One hypothetical use for such a filter 
might be to normalize state names that would be tokenized as two words. For example, the single token 'California' would be replaced with 'CA', while the token pair 
'Rhode' followed by 'island' would become the single token 'RI'.

Because filters consume one Token Stream and produce a new Token Stream, they can be chained one after another indefinitely. Each filter in the chain in turn 
processes the tokens produced by its predecessor. The order in which you specify the filters is therefore significant. Typically, the most general filtering 
is done first, and later filtering stages are more specialized.

.. rst-class:: ui celled table
.. list-table:: Filter Properties
   :header-rows: 1
   :widths: 10 40
   :stub-columns: 1

   *  -  Property Name
      -  Description
   *  -  ``FilterName``
      -  The name of the filter
   *  -  ``Parameters``
      -  Configurable parameters which can be supplied to a tokenizer.

.. code-block:: c

*/
	struct TokenFilter {
		1:	required string FilterName
		2:	optional map<string, string> Parameters
	}
//>

/*
<Tokenizer
^^^^^^^^^^^^

The job of a tokenizer is to break up a stream of text into tokens, where each token is (usually) a sub-sequence of the characters in the text. An analyzer is aware of the field it 
is configured for, but a tokenizer is not. Tokenizers read from a character stream (a Reader) and produce a Characters in the input stream may be discarded, such as white-space 
or other delimiters. They may also be added to or replaced, such as mapping aliases or abbreviations to normalized forms. A token contains various meta-data in addition to its text value, 
such as the location at which the token occurs in the field. Because a tokenizer may produce tokens that diverge from the input text, you should not assume that the text of the token 
is the same text that occurs in the field, or that its length is the same as the original text. It's also possible for more than one token to have the same position or refer to the 
same offset in the original text. Keep this in mind if you use token meta-data for things like highlighting search results in the field text.

.. rst-class:: ui celled table
.. list-table:: Tokenizer Properties
   :header-rows: 1
   :widths: 10 40
   :stub-columns: 1

   *  -  Property Name
      -  Description
   *  -  ``TokenizerName``
      -  The name of the tokenizer
   *  -  ``Parameters``
      -  Configurable parameters which can be supplied to a tokenizer.

.. code-block:: c

*/
	struct Tokenizer {
		1:	required string TokenizerName
		2:	optional map<string, string> Parameters
	}
//>

/*
<Analyzer
^^^^^^^^^^

An analyzer examines the text of fields and generates a token stream. Analyzers are specified as part of the Field Properties element in the Fields section of index configuration.

Only the following field types can have an analyzer, if specified the analyzer will be ignored for other field types.

- Text
- Highlight
- Custom

.. note:: 
    StandardAnalyzer is used when no analyzer is specified.
    
For simple cases, such as plain English prose, a single analyzer class like this may be sufficient. But it's often necessary to do more complex analysis of the field content. Even the 
most complex analysis requirements can usually be decomposed into a series of discrete, relatively simple processing steps. As you will soon discover, the FlexSearch distribution 
comes with a large selection of tokenizers and filters that covers most scenarios you are likely to encounter.

Refer to FlexSearch Analysis for examples of supported tokenizers and filters.

.. rst-class:: ui celled table
.. list-table:: Analyzer Properties
   :header-rows: 1
   :widths: 10 40
   :stub-columns: 1

   *  -  Property Name
      -  Description
   *  -  ``Tokenizer``
      -  :ref:`Tokenizer <Tokenizer>`
   *  -  ``TokenFilter``
      -  list of :ref:`TokenFilter <TokenFilter>`

.. code-block:: c

*/
	struct AnalyzerProperties {
		1:	required Tokenizer Tokenizer
		2:	required list<TokenFilter> Filters
	}
//>

// ----------------------------------------------------------------------------
//	Scripting related
// ----------------------------------------------------------------------------
/*
<Script Properties
^^^^^^^^^^^^^^^^^^^^

.. code-block:: c

*/
	struct ScriptProperties {
		1:	required string Source
		2:	required ScriptType ScriptType
	}
//>

// ----------------------------------------------------------------------------
//	Search related
// ----------------------------------------------------------------------------
/*
<Missing Value Option
^^^^^^^^^^^^^^^^^^^^^

Refer to 'Search profile basics'.

.. code-block:: c

*/
	enum MissingValueOption {
		ThrowError = 1
		Default = 2
		Ignore = 3
	}
//>

struct MissingValue {
	1:	required MissingValueOption MissingValueOption
	2:	optional string DefaultValue
}

/*
<HighlightOption
^^^^^^^^^^^^^^^^^^^^^^

Refer to 'Search basics'.

.. code-block:: c

*/
	struct HighlightOption {
		1:	optional i32 FragmentsToReturn = 2
		2:	required list<string> HighlightedFields
		3:	optional string PostTag = "</B>"
		4:	optional string PreTag = "</B>"
	}
//>

/*
<Search Profile
^^^^^^^^^^^^^^^^^

A Search profile is a user configurable search criteria which can be saved as a
part of index configuration enabling an user to pass a set of values to match 
against it. Search profile utilizes the ``Search Query`` object to define a profile.
In nutshell any valid query can be saved as a search profile. Search profile uses a
special configuration called ``MissingValueConfiguration`` which allows it to 
respond to missing values in the passed ``QueryString`` thus giving complete control
over how the query will be parsed by the server.

** Query String Format for Search profile**

Search profile expects the caller to populate ``SearchProfile`` and ``QueryString`` properties.
The Search profile ``QueryString`` uses a specialized format to pass key value pairs.

.. code:: javascript

    {fieldName1: 'fieldValue1', fieldName2 : 'fieldValue2', ..}

*/
//>

/*
<Search Query
^^^^^^^^^^^^^

**Columns** 
Columns to be returned as a part of search result. Use ``*`` to return all columns. 
Specifying no columns returns nothing. Note: You still get id, lastmodified and type column back.

Columns can also be specified as a part of query string by using ``c=field1;field2,..`` etc.

Note
	The format used by ``c`` and ``Columns`` is different. One uses a set of ``,`` separated fields
	while the other uses a List of string. The reason for having two different representation is
	is due to the fact that one allows easy option to add fields programatically while the other
	is easy for use with JavaScript.
	
**Count**
The number of results to be returned. Can also be passed as a part of query string as ``count``.

**OrderBy**
The field which is used to sort the results. But default the results are 
ordered by relevance.  Can also be passed as a part of query string as ``orderby``.

**Skip**
The total number of records to be skipped from the top. Useful for 
implementing paging.  Can also be passed as a part of query string as ``skip``.

**QueryString**
The search query to be executed.  Can also be passed as a part of query string as ``q``.

**ReturnFlatResult**
Return the results as simple json array enabling easy binding to the a grid.
Can also be passed as a part of query string as ``returnflatresult``.

**ReturnScore**
Return score as a part of search result. In case you are using ``ReturnFlatResult`` then the 
score will be returned in ``_score`` field.

**SearchProfile**
Pass the name of the search profile in case of profile based searching.

.. code-block:: c

*/
	struct SearchQuery {
		1:	optional list<string> Columns = {}
		2:	optional i32 Count = 10
		3:	optional HighlightOption Highlights
		4:	required string IndexName
		5:	optional string OrderBy = "score"
		6:	optional i32 Skip = 0
		7:	required string QueryString
		8:	optional map<string, MissingValueOption> MissingValueConfiguration = {}
		9:	optional MissingValueOption GlobalMissingValue = 1
		10:	optional bool ReturnFlatResult = false
		11:	optional bool ReturnScore = true
		12: optional string SearchProfile
		13: optional string SearchProfileSelector
	}
//>

// ----------------------------------------------------------------------------
//	Server Settings
// ----------------------------------------------------------------------------
/*
<Server Settings
^^^^^^^^^^^^^^^^^

Represents server settings to be defined in ``conf.json``. These are loaded initially when the server 
loads.

.. code-block:: c

*/
	struct ServerSettings {
		1:	optional i32 HttpPort = 9800
		2:	optional i32 ThriftPort = 9900
		3:	optional string DataFolder = "./data"
		4:	optional string PluginFolder = "./plugins"
		5:	optional string ConfFolder = "./conf"
		6:	optional string NodeName = "FlexNode"
		7:	optional NodeRole NodeRole = 1
		8:	optional string Logger = "Gibraltar"
	}
//>

// ----------------------------------------------------------------------------
//	Index & Document related
// ----------------------------------------------------------------------------
/*
<Document
^^^^^^^^^^

Represents the result document returned from Search service when ``Returnflatresult`` parameter is set
to false. This offers more structured result compared to Flat results. This is the only supported output
when Highlight is used.

.. code-block:: c

*/
	struct Document {
		1:	optional map<string, string> Fields = {}
		2:	optional list<string> Highlights = {}
		3:	required string Id
		4:	optional i64 LastModified
		7:	required string Index
		8:	optional double Score = 0.0
	}
//>

/*
<Index
^^^^^^^^^^

In case of a database analogy an index represents a table in a database where one has to define a schema upfront before performing any kind of operation on the table. 
There are various properties that can be defined at the index creation time. Only ``IndexName`` is a mandatory property, though one should always define ``Fields`` in an index to make any use of it.

By default a newly created index stays offline. This is by design to force the user to enable
an index before using it.

.. rst-class:: ui celled table
.. list-table:: Index Properties
   :header-rows: 1
   :widths: 10 40
   :stub-columns: 1

   *  -  Property Name
      -  Description
   *  -  ``IndexName``
      -  Name of the index
   *  -  ``Online``
      -  Status of the index. An index should be on-line in order to enable searching over it.
   *  -  ``Analyzers``
      -  map of :ref:`Analyzer <Analyzer>`
   *  -  ``IndexConfiguration``
      -  :ref:`IndexConfiguration <IndexConfiguration>`
   *  -  ``FieldProperties``
      -  map of :ref:`FieldProperties <FieldProperties>`
   *  -  ``ScriptProperties``
      -  map of :ref:`ScriptProperties <FieldProperties>`
   *  -  ``SearchProfiles``
      -  map of :ref:`SearchQuery <SearchQuery>`
   *  -  ``ShardConfiguration``
      -  :ref:`ShardConfiguration <ShardConfiguration>`

.. code-block:: c

*/
	struct Index {
		1:	optional map<string, AnalyzerProperties> Analyzers = {}
		2:	required IndexConfiguration IndexConfiguration = {}
		3:	required map<string, FieldProperties> Fields = {}
		4:	required string IndexName
		5:	required bool Online = false
		6:	optional map<string, ScriptProperties> Scripts = {}
		7:	optional map<string, SearchQuery> SearchProfiles = {}
		8:	required ShardConfiguration ShardConfiguration = {}
	}
/*
.. include:: model-analyzer.rst
.. include:: model-tokenizer.rst
.. include:: model-tokenfilter.rst
.. include:: model-fieldproperties.rst
.. include:: model-fieldtype.rst
.. include:: model-IndexConfiguration.rst
.. include:: model-ScriptProperties.rst
.. include:: model-ShardConfiguration.rst
.. include:: model-SearchProfile.rst
.. include:: model-SearchQuery.rst
*/
//>

/*
<Search Results
^^^^^^^^^^^^^^^^^^^

Used to return structured search results. This is useful in case when there is a requirement to merge results from
multiple indices or sort on scores. The structured result can be de-serialized and sorted in any
programming language.

There is an option to get flat results from the rest service by using the ``Returnflatresult`` parameter which can 
be easily bound to a grid.

.. code-block:: c

*/
	struct SearchResults {
		1:	optional list<Document> Documents = {}
		2:	optional i32 RecordsReturned
		3:	optional i32 TotalAvailable
	}
//>

/*
<Filter List
^^^^^^^^^^^^^^^^^^^

.. code-block:: c

*/
	struct FilterList {
		1:	required list<string> Words = {}
	}
//>

/*
<Map List
^^^^^^^^^^^^^^^^^^^

.. code-block:: c

*/
	struct MapList {
		1:	required map<string, list<string>> Words = {}
	}
//>


struct IndexStatusResponse {
	1:	required IndexState Status
}

struct ImportRequest {
	1:	optional string Id
	2:	optional map<string,string> Parameters = {}
	3:	optional bool ForceCreate = false
	4:	optional string JobId
}

struct ImportResponse {
	1:	optional string JobId
	2:	optional string Message
}