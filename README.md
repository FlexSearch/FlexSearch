FlexSearch
==========

FlexSearch is a high performance REST/SOAP services based full-text searching platform built on top of the popular Lucene search library.  At its core it is about extensibility and maintainability with minimum overhead. 
FlexSearch is written in F# & C# 5.0 (.net framework 4.5). It exposes REST, SOAP and Binary based web service endpoints enabling easy integration. It has an extensive plug-in architecture with ability to customize most of the functionality with minimum amount of efforts. One area where FlexSearch particularly excel over competition is providing easy extensible connector model which allows a developer to tap directly into core’s indexing engine, thus avoiding the reliance on web services. This results in a greatly improved indexing performance when indexing over millions of records.

More information is available at http://www.flexsearch.net/


**This is a pre-release version and subjected to major changes.** 

There are a number of features which are in progress:

- Optimistic concurrency
- PerField Postings Format support
- ~~Proper Http status codes support (Dropping support for SOAP as it is screwing up the REST endpoint). Hopefully once this work is done then the whole API will be very easy to understand.~~
- Complete documentation and a testing tool
- Configuration UI
- ~~Upgrading to F# 3.1 (some specific features are of particular interest)~~
- ~~Move toward NancyFx~~
- ~~New SQL like search syntax~~
- ~~Move towards more F# like code structure. It is too Object oriented at the moment.~~


**Update 27/02/2013**

- A lot of work is already completed in the 0.21 release branch. Everything is coming together and I am hoping for an end of March release of alpha edition. This will not be the distributed version but will have stable web services and a stable core to build upon. 
- Most of the work to make FlexSearch distributed is also complete but the feature won't be exposed till 0.30 release.
