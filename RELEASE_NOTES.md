### Release - 0.7.2-beta (04-09-2016)

#### Feat
* [[4e375d0]](https://github.com/flexsearch/flexsearch/commit/4e375d0fb191093003a1005b75cf06abf21a4451) Strongly sign the FlexSearch

### Release - 0.7.1-beta (26-08-2016)

#### Fix
* [[66b315c]](https://github.com/flexsearch/flexsearch/commit/66b315ca01671385857ed7ea5efd9eb767523ce6) Remove dependecy on external reference urls
* [[073c187]](https://github.com/flexsearch/flexsearch/commit/073c1874596d81838a057767153864341f855831) Minor UI improvement to the top bar of portal
* [[1f0bb27]](https://github.com/flexsearch/flexsearch/commit/1f0bb2733bbc10cc821eb393bf767598efb581ac) Use predef query highlights when searching

#### Feat
* [[01c2eac]](https://github.com/flexsearch/flexsearch/commit/01c2eacfa5fc2c9128d567c0cdadc119d4a9ade2) Add highlights option to search profile in adminui

### Release - 0.7.0-beta (04-08-2016)

#### Fix
* [[87bc24d]](https://github.com/flexsearch/flexsearch/commit/87bc24d1bf7c466f61ec83d5f50f2d103f7e074a) Stop using fieldType header on adminui
* [[4049f0e]](https://github.com/flexsearch/flexsearch/commit/4049f0e4da1176f4eb397b5b854e0a89775e5145) Relabel the Quick Add adminui tool
* [[591d37f]](https://github.com/flexsearch/flexsearch/commit/591d37f107dc661ab72a5b307825b7029a90476d) Fix System
* [[adf4149]](https://github.com/flexsearch/flexsearch/commit/adf41493a85f4f7e93aad49f331b85a4880d6513) Verify predefined query is valid before adding it
* [[6f25f30]](https://github.com/flexsearch/flexsearch/commit/6f25f30e2e66109ef091dc6a3377d660b1dcc108) Paket no longer supports framework auto
* [[5ad2476]](https://github.com/flexsearch/flexsearch/commit/5ad2476eff67bddfa02995372e4a199f76ab28ff) Allow updating an already closed index
* [[ca78c03]](https://github.com/flexsearch/flexsearch/commit/ca78c03d35a4b57f4072f2f84622026dd6b7d5fc) Keep index offline when IndexLoadingFailure
* [[57182ec]](https://github.com/flexsearch/flexsearch/commit/57182ecc886df9a0d54e397810a644c025ccb01d) Don
* [[2fe2686]](https://github.com/flexsearch/flexsearch/commit/2fe26869e9693a2dfe4fff3a047c7591d5e81035) Allow dashboard to process offline indices as well
* [[ec960b1]](https://github.com/flexsearch/flexsearch/commit/ec960b18047821195819a32fd5170e9a83c12cdc) Remove httplistener completely and start using aspnetcore rc2 based extension method for configuring server
* [[3d6fce4]](https://github.com/flexsearch/flexsearch/commit/3d6fce47e008cd1fa95899acce3f4f00afe6ea1b) Fix deserialization problem during demo index setup
* [[1235d8b]](https://github.com/flexsearch/flexsearch/commit/1235d8b97311012248a62f0ef20cc23f36e7a168) Fix missing dlls when running scripts

#### Feat
* [[570709d]](https://github.com/flexsearch/flexsearch/commit/570709ddc8a249e0f71b4183b829bce056710b73) Minor UI improvements for adminui
* [[0ba6c45]](https://github.com/flexsearch/flexsearch/commit/0ba6c45bf4964f5ce528b1b0dca3e21376a7fe25) Implement quickAdd tool for adminui
* [[89132e8]](https://github.com/flexsearch/flexsearch/commit/89132e83281da7b9a1008f21f2fd5ef5d17f5c1a) Present lonely screen when no indices are present
* [[cac8a3b]](https://github.com/flexsearch/flexsearch/commit/cac8a3b08e82fe07f717f0476cb99148a0914f58) Implement editing predefined queries
* [[721c26a]](https://github.com/flexsearch/flexsearch/commit/721c26a4f931c55e2274312255c2287f7517323d) Add shard configuration settings in adminui
* [[f929385]](https://github.com/flexsearch/flexsearch/commit/f929385fcba74daa02a9da4d4e66706bfd39c6d8) Implement index configuration view for adminui
* [[d0d28b8]](https://github.com/flexsearch/flexsearch/commit/d0d28b84190d9fff899acd69039d0123fcfdfded) Implement editing all field properties in adminui
* [[d851c35]](https://github.com/flexsearch/flexsearch/commit/d851c3550cd519a6e545988e99a2de0559f2e709) Add field part of index
* [[5608f6b]](https://github.com/flexsearch/flexsearch/commit/5608f6b5f8e5f7f4499f6cf9b3f042e2f9f9798f) Implement basic commands on indexDetails page in adminui
* [[8c168f2]](https://github.com/flexsearch/flexsearch/commit/8c168f21987903248bf5d745774660522421709a) Expose refreshing an index as an HTTP service
* [[6a93566]](https://github.com/flexsearch/flexsearch/commit/6a93566cc1ed9e53e9a6e82e743c975dab5b93bd) Implement newIndex sidenav
* [[85d3029]](https://github.com/flexsearch/flexsearch/commit/85d3029ee25c2963cbcbd953dbece52c48e3c13d) Initial commit of overview page in adminui

#### Refactor
* [[d54cb75]](https://github.com/flexsearch/flexsearch/commit/d54cb7506dd5dcd1ffada9bb4016b620ba9887cc) Remove protobuf dependency as message pack is the preferred binary format

#### Docs
* [[f37ebe6]](https://github.com/flexsearch/flexsearch/commit/f37ebe6f21cc8197473d15d608daa6ede7b5ba5b) Fix the icon image page
* [[dc7e4d7]](https://github.com/flexsearch/flexsearch/commit/dc7e4d745b91e38c37da3cf30e7f87ef683a9e77) Update image folder location to match new site structure
* [[243ac95]](https://github.com/flexsearch/flexsearch/commit/243ac952111104fbd2fc8be8fcfd42da00ca5fea) Fix generated html by adding proper closing tags

#### Tests
* [[308a2ca]](https://github.com/flexsearch/flexsearch/commit/308a2caa9a97bbd3f798fb26789d71bb0e99a0c1) Enabled text highlighting example to dump request so that iit can be used in documentation

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
