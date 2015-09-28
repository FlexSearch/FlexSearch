# Summary

* [Getting Started](getting-started/getting-started.md)

* Essential Path
  * [Installing FlexSearch](server-setup/installing.md)
  * [Setting up demo country index](demo-index/setting-up-demo-index.md)
  * [Search using the UI](demo-index/search-ui.md)
  * [REST services basics](rest-basics)

* Suggested Path
  * [Installing FlexSearch](server-setup/installing.md)
  * [Configuring FlexSearch](server-setup/configuring.md)
  * [Setting up demo country index](demo-index/setting-up-demo-index.md)
  * [Search basics](search-basics)
  * [Search using the UI](demo-index/search-ui.md)
  * [REST services basics](rest-basics)
  * [Creating an index](creating-an-index)
  * [Creating custom analyzers](creating-custom-analyzers)
  * [Scripting basics](scripting-basics)
  * [Adding data to an index](creating-an-index)

* Recipes & Guides
  * [Configuring](getting-started/configuring.md)
  * [Installing](getting-started/installing.md)
  * Troubleshooting
  * [Telephone number cleanup](getting-started/installing.md)
  * [Synonyms](getting-started/installing.md)
  * [Phonetic matching](getting-started/installing.md)
  * Postcode searching

* Server Setup
  * [Installing FlexSearch](server-setup/installing.md)
  * [Configuring FlexSearch](server-setup/configuring.md)

* Concepts
  * Fields
  * Analysis
    - Resources
    - different stages when analysis is done: index time, search time, pre/post search filtering
  * Filtering
  * Indexing
    - How data is being indexed
    - concurrency clash resolution
    - inverted index, docvalues (searching by doc id)
    - sorting
    - searching by doc id vs term search
    - index statuses
  * Connectors
  * Storing data
    - Refresh / Commit
    - DirectoryType
    - Sharding  
  * Scripting
  * Transaction Log
  * Logging - ETW, log folder during startup

* REST API
    * Index Services
    * Document Services
    * Search
    * Analyzers

* Search
  * Basic Concept
  * Filtering
  * [Search Profile]
    - syntax
    - examples with different syntax
  * Flat Results vs List of Documents
  * Search DSL
    * [Term Match](search-dsl/operator-types/term-match.md)
    * [Phrase Match](search-dsl/operator-types/phrase-match.md)
    * [Fuzzy](search-dsl/operator-types/fuzzy.md)
    * [Wildcard](search-dsl/operator-types/wildcard.md)
    * [Regex](search-dsl/operator-types/regex.md)
    * [Numeric range](search-dsl/operator-types/numeric-range.md)
    * [Match all](search-dsl/operator-types/matchall.md)
    * Function searching
* Import Handlers
    * CSV
    * SQL

* Portal
    - Swagger
* BDM
    - process.js
    - duplicates index

* Extending FlexSearch
    * Understanding Services
    * Writing HTTP Endpoints
    * Writing Query operators
    * Writing Search Functions
* FAQ
* Glossary
* Credits
