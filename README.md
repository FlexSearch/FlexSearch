FlexSearch
==========

[![Build status](https://ci.appveyor.com/api/projects/status/lv07ggb6pxxt4dtg/branch/master?svg=true)](https://ci.appveyor.com/project/seemantr/flexsearch/branch/master)

FlexSearch is a high performance REST/SOAP services based full-text searching platform built on top of the popular Lucene search library.  At its core it is about extensibility and maintainability with minimum overhead. 
FlexSearch is written in F# & C# 5.0 (.net framework 4.6). It exposes REST, SOAP and Binary based web service endpoints enabling easy integration. It has an extensive plug-in architecture with ability to customize most of the functionality with minimum amount of efforts. One area where FlexSearch particularly excel over competition is providing easy extensible connector model which allows a developer to tap directly into core’s indexing engine, thus avoiding the reliance on web services. This results in a greatly improved indexing performance when indexing over millions of records.

More information is available at http://www.flexsearch.net/


### Build FlexSearch

#### Prerequisites
- Windows machines only
- Visual Studio 2015
- Java installed on your machine. Make sure the `JAVA_HOME` system environment variable is set up.
- NodeJS

In order to build FlexSearch you need to run the following commands:
```
> git submodule update --init --recursive
> .\build
```
