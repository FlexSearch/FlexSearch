### 0.6.9-beta - FlexSeach 0.6.9-beta
* Switch to only using the Nuget package source v3

#### 0.6.8-beta - FLexSearch 0.6.8-beta
* Move packages to aspnet RC2

#### 0.6.7-beta - FlexSeach 0.6.7-beta
* Include System.Reflection.dll into the final build package

#### 0.6.6-beta - FlexSeach 0.6.6-beta
* Redirect Newtonsoft.Json to version 8.0.0.0

#### 0.6.5-beta - FlexSearch 0.6.5-beta
* Copy libuv.dll from new package path
* Handle SearchQuery validation gracefully
* Field validation error message should mention missing field
* Include System.Reflection in build folder
* Don't require IndexName in DTO when creating documents
* Update to latest aspnet packages, including changing from Microsoft.AspNet.* to Microsoft.AspNetCore.*
* Check for JAVA_HOME existance during build
* Don't commit paket.exe
* Add new icon for flexsearch-server.exe
* Delete country index folder in case it already exists
* Rationalize all existing tests and add missing tests for phrase query

#### 0.6.4-beta - FlexSearch 0.6.4-beta
* Update NOTICE and LICENSE files
* Run paket simplify

#### 0.6.3-beta - FlexSearch 0.6.3-beta
* Implement IDisposable for RealTimeSearcher

#### 0.6.2-beta - FlexSearch 0.6.2-beta
* Move from Nuget to Paket
* Remove Fody
* Automate github release

#### 0.6.1-beta - March 16 2013
* Pre-index and pre-search scripting fixes

#### 0.6.0-beta - March 16 2013
* Include CSV + SQL connector

#### 0.5.1-beta - March 16 2016
* Major rewrite of FlexSearch
