# Parameters
Key value pair to be used to configure object's properties.

# Similarity
Similarity defines the components of scoring. Similarity determines how 
engine weights terms. FlexSearch interacts with Similarity at both index-time 
and query-time.

# DirectoryType
A Directory is a flat list of files. Files may be written once, when they are 
created. Once a file is created it may only be opened for read, or deleted. 
Random access is permitted both when reading and writing.

# TermVector
These options instruct FlexSearch to maintain full term vectors for each document, 
optionally including the position and offset information for each term occurrence 
in those vectors. These can be used to accelerate highlighting and other ancillary 
functionality, but impose a substantial cost in terms of index size. These can 
only be configured for custom field type.

# IndexOptions
Controls how much information is stored in the postings lists.

# IndexVersion
Corresponds to Lucene Index version. There will always be a default codec 
associated with each index version.

# DataType
The field type defines how FlexSearch should interpret data in a field and how 
the field can be queried. There are many field types included with FlexSearch 
by default and custom types can also be defined.

# Tokenizer
Tokenizer breaks up a stream of text into tokens, where each token is a sub-sequence
of the characters in the text. An analyzer is aware of the field it is configured 
for, but a tokenizer is not.

# Filter
Filters consume input and produce a stream of tokens. In most cases a filter looks 
at each token in the stream sequentially and decides whether to pass it along, 
replace it or discard it. A filter may also do more complex analysis by looking 
ahead to consider multiple tokens at once, although this is less common.

# Analyzer
An analyzer examines the text of fields and generates a token stream.

# Fields
Represents a group of fields.

# Field
A field is a section of a Document. 

Fields can contain different kinds of data. A name field, for example, 
is text (character data). A shoe size field might be a floating point number 
so that it could contain members like 6 and 9.5. Obviously, the definition of 
fields is flexible (you could define a shoe size field as a text field rather
than a floating point number, for example), but if you define your fields correctly, 
FlexSearch will be able to interpret them correctly and your users will get better 
results when they perform a query.

You can tell FlexSearch about the kind of data a field contains by specifying its 
field type. The field type tells FlexSearch how to interpret the field and how 
it can be queried. When you add a document, FlexSearch takes the information in 
the document’s fields and adds that information to an index. When you perform a 
query, FlexSearch can quickly consult the index and return the matching documents.

# SearchQuery
Search query is used for searching over a FlexSearch index. This provides
a consistent syntax to execute various types of queries. The syntax is similar
to the SQL syntax. This was done on purpose to reduce the learning curve.

# Document
A document represents the basic unit of information which can be added or 
retrieved from the index. A document consists of several fields. A field represents 
the actual data to be indexed. In database analogy an index can be considered as 
a table while a document is a row of that table. Like a table a FlexSearch document 
requires a fix schema and all fields should have a field type.

# Index
FlexSearch index is a logical index built on top of Lucene’s index in a manner 
to support features like schema and sharding. So in this sense a FlexSearch 
index consists of multiple Lucene’s index. Also, each FlexSearch shard is a memberid 
Lucene index.

In case of a database analogy an index represents a table in a database where 
one has to define a schema upfront before performing any kind of operation on 
the table. There are various properties that can be defined at the index creation 
time. Only IndexName is a mandatory property, though one should always define 
Fields in an index to make any use of it.

By default a newly created index stays off-line. This is by design to force the 
user to enable an index before using it.

# Index Name
The name of the index on which the operation is to be performed.

# ModifyIndex
Represents the operation number associated with the operation in the global order 
of the operations. This allows causal ordering of the events. A documents with a lower
ModifyIndex can be assumed to be modified before another with a higher number. ModifyIndex
is used for optimistic concurrency control.

# Shard Configuration
Allows to control various Index Shards related settings.

# Index Configuration
Allows to control various Index related settings.