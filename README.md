FlexSearch
==========

FlexSearch is a high performance REST/SOAP services based full-text searching platform built on top of the popular Lucene search library.  At its core it is about extensibility and maintainability with minimum overhead. 
FlexSearch is written in F# & C# 5.0 (.net framework 4.5). It exposes REST, SOAP and Binary based web service endpoints enabling easy integration. It has an extensive plug-in architecture with ability to customize most of the functionality with minimum amount of efforts. One area where FlexSearch particularly excel over competition is providing easy extensible connector model which allows a developer to tap directly into core’s indexing engine, thus avoiding the reliance on web services. This results in a greatly improved indexing performance when indexing over millions of records.

More information is available at http://www.flexsearch.net/


**This is a pre-release version and might introduce breaking changes.** 

**Roadmap**

Most of the road map is suggestive at the moment and might change depending upon other requirements. The focus is to have a stable bug free core even at the expense of reduced functionality.

- 0.21 is already out which marks the release of a stable core.

- 0.22 (To be released)
  - There are around 400 tests at the moment but there are still few hotspots in the code base which are not covered through tests. The major goal of this release will be to plug all those gaps and have a 100 percent testable code base.
  - More examples: There are around 100 examples in the documentation but a lot more could be done to improve the quality of those examples.
  - Bug fixes 
  - Stable API for connector model
  - SQL and CSV connectors
  - Add stress tests
  - Finalize Wikipedia based tests
  - Add FSCheck based random testing
  - Add analysis service
  - Create a homepage at the server root to display basic information. At the moment it just reports FlexSearch with version number.
  
- 0.23 (TBR)
  -   Release duplicate detection studio application (free WPF application)
  -   Release Background duplicate matching functionality

-   0.25 (TBR)
  - Freeze all API for version 1.0     

- 1.1 (TBR)
  - A lot of work is already completed in the 0.21 release branch. Most of the work to make FlexSearch distributed is also complete but the feature won't be exposed till 0.30 release and will slowly be baked into the product after 1.0 release.
