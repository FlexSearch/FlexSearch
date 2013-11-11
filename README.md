FlexSearch
==========

FlexSearch is a high performance REST/SOAP services based full-text searching platform built on top of the popular Lucene search library.  At its core it is about extensibility and maintainability with minimum overhead. 
FlexSearch is written in F# & C# 5.0 (.net framework 4.5). It exposes REST, SOAP and Binary based web service endpoints enabling easy integration. It has an extensive plug-in architecture with ability to customize most of the functionality with minimum amount of efforts. One area where Lunar particularly excel over competition is providing easy extensible connector model which allows a developer to tap directly into core’s indexing engine, thus avoiding the reliance on web services. This results in a greatly improved indexing performance when indexing over millions of records.

More information is available at http://www.flexsearch.net/


**This is a pre-release version and subjected to major changes.** 

There are a number of features which are in progress:

- Optimistic concurrency
- PerField Postings Format support
- Proper Http status codes support (Dropping support for SOAP as it is screwing up the REST endpoint). Hopefully once this work is done then the whole API will be very easy to understand.
- Complete documentation and a testing tool
- Configuration UI
- Upgrading to F# 3.1 (some specific features are of particular interest)
- Move toward NancyFx

Please see the [Future Roadmap](roadmap) for more details and timelines. 


Update 11/11/2013
====================

- After initial feedback from few people my understanding is that FlexSearch should be distributed in nature. Based on this new requirement I have started adding distributed capabilities to the core. At technical level I have figured out most of the things. It also makes sense to do it now as the product is still in its early phase.
- All the updates are happening in NancyFx branch.
- I am still looking at end of December to release a fully functioning alpha version.
