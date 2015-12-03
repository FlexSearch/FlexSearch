/// <reference path="..\references\references.d.ts" />

module flexportal {
	
	import SearchResults = FlexSearch.Core.SearchResults;
	import FieldDto = FlexSearch.Core.FieldDto;
	import IndexConfigurationDto = FlexSearch.Core.IndexConfigurationDto;
	import MemoryDetailsResponse = FlexSearch.Core.MemoryDetailsResponse;
	import Analyzer = FlexSearch.Core.Analyzer;
	
	export class IndexResult {
		Fields: FieldDto[]
		IndexConfiguration: IndexConfigurationDto
		SearchProfiles: any[]
		ShardConfiguration: any
		IndexName: string
		Online: boolean
	}
	
	export class FlexClient {
		private $http: ng.IHttpService;
		private $mdBottomSheet: any;
		private $q: any;
		private handleError: any;
		public FlexSearchUrl: string;
		public DuplicatesUrl: string;
		
        /* @ngInject */
		constructor($http: ng.IHttpService, $mdBottomSheet: any, $q: any, $location: any) {
			 this.$http = $http;
			 this.$mdBottomSheet = $mdBottomSheet;
			 this.$q = $q;
			 this.FlexSearchUrl = "http://localhost:9800";
			 // If the host is local then use the hard coded url as we might be testing
			 // the ui
			 if ($location.host() != "localhost")
				 this.FlexSearchUrl = $location.protocol() + "://" + $location.host() + ":" + $location.port();
			 this.DuplicatesUrl = this.FlexSearchUrl + "/indices/duplicates";
			 this.handleError = 
			 	function(bs, q) {
					return errorHandler
						.bind(null, q)
						.bind(null, bs);
				} (this.$mdBottomSheet, this.$q);
		}
		
		private static getSearchResults (response : any) {
			return <SearchResults>response.data.Data;
		}
		
		private getFirstResponse (results: SearchResults) {
			// Get the first response
			if (results.Documents.length != 1) { 
				this.handleError("No results returned."); 
				return null; 
			}
			
			return results.Documents[0];
		}
		
		private static getData (response: any) {
			return response.data.Data;
		}
		
		private static getFlatResults (response: any) {
			return <string[]>FlexClient.getData(response);
		}
		
		public getRecordById(indexName, id) {
			var url = this.FlexSearchUrl + "/indices/" + indexName + "/documents/" + id + "?c=*";
			return this.$http.get(url)
				.then(
					response => <FlexSearch.Core.DocumentDto>(<any>response.data).Data,
					this.handleError);
	  	}
		  
	  	public getRecordsByIds(indexName, ids: any []) {
			var idStr = ids
				.reduce(function (acc, val) { return acc + ",'" + val + "'" }, "")
				.substr(1);
		  	return this.$http.get(this.FlexSearchUrl + "/indices/" + indexName + "/search", { params: 
				  {
					 q: "_id eq [" + idStr + "] {clausetype: 'or'}",
					 c: "*",
					 returnFlatResult: "true"
				  } })
			  .then(FlexClient.getFlatResults);
	  	}
		  
	  	// Get the duplicate that needs to be displayed
		public getDuplicateBySourceId(sessionId, sourceId) {
			return this.$http.get(this.DuplicatesUrl + "/search", {params: {
				q: "type = 'source' and sessionid = '" + sessionId + "' and sourceid = '" + sourceId + "'",		
				c: "*" }}
			)
			.then(FlexClient.getSearchResults)
	      	.then(this.getFirstResponse, this.handleError);
	  	}
		  
	  	public updateDuplicate(duplicate: Duplicate) {
			return this.$http.put(this.DuplicatesUrl + "/documents/" + duplicate.FlexSearchId,
			{
				Fields: {
				  sessionid: duplicate.SessionId,
				  sourcedisplayname: duplicate.SourceDisplayName,
				  sourceid: duplicate.SourceId,
				  sourcerecordid: duplicate.SourceRecordId,
				  totaldupesfound: duplicate.TotalDupes,
				  type: "source",
				  notes: duplicate.Notes,
				  sourcestatus: duplicate.SourceStatus,
				  targetrecords: JSON.stringify(duplicate.Targets)
				},
				Id: duplicate.FlexSearchId,
				IndexName: "duplicates"
			});
	  	}
		  
		public getSessionBySessionId(sessionId) {
		  return this.$http.get(this.DuplicatesUrl + "/search", 
		        { params: { 
		            q: "type = 'session' and sessionid = '" + sessionId + "'",
		            c: "*" } }
		    )
		    .then(FlexClient.getSearchResults)
			.then(this.getFirstResponse, this.handleError);
		} 
		
		public getDuplicatesFromSession(sessionId, count, skip, sortby?, sortDirection?) {
            return this.$http.get(this.DuplicatesUrl + "/search", 
                { params: { 
                    q: "type = 'source' and sessionid = '" + sessionId + "'",
                    c: "*",
                    count: count,
                    skip: skip,
					orderBy: sortby,
			  		orderByDirection: sortDirection }})
			.then(FlexClient.getSearchResults, this.handleError);
		}
		
		public getSessions(count, skip, sortby?, sortDirection?) {
			
			return this.$http.get(this.DuplicatesUrl + "/search", { params: {
	          c: "*",
	          q: "type = 'session'",
	          skip: skip,
	          count: count,
			  orderBy: sortby,
			  orderByDirection: sortDirection
	        }})
			.then(FlexClient.getSearchResults, this.handleError);
		}
		
		public getIndices() {
			return this.$http.get(this.FlexSearchUrl + "/indices")
				.then(FlexClient.getData)
				.then(result => <IndexResult []> result, this.handleError);
		}
		
		public submitDuplicateDetection(indexName, searchProfile, displayFieldName, selectionQuery, fileName,
			threadCount?, maxRecordsToScan?, maxDupsToReturn?) {
			return this.$http.post(this.FlexSearchUrl + "/indices/" + indexName + "/duplicatedetection/" + searchProfile, {
					DisplayName: displayFieldName,
					SelectionQuery: selectionQuery,
					FileName: fileName,
					ThreadCount: threadCount,
					MaxRecordsToScan: maxRecordsToScan,
					DuplicatesCount: maxDupsToReturn
				});
		}
		
		public submitSearchProfileTest(indexName, searchQueryString, searchProfileQueryString, 
			columnsToRetrieve?: string[], count?, skip?) {
			return this.$http.post(this.FlexSearchUrl + "/indices/" + indexName + "/searchprofiletest", {
				SearchQuery: {
					QueryString: searchQueryString,
					Columns: columnsToRetrieve.join(",") || "*",
					Count: count,
					Skip: skip
				},
				SearchProfile: searchProfileQueryString
			})
			.then(FlexClient.getData, this.handleError);
		}
		
		public submitSearch(indexName, searchQueryString, columnsToRetrieve?: string[], count?, skip?,
			orderBy?, orderByDirection?) {
			return this.$http.get(this.FlexSearchUrl + "/indices/" + indexName + "/search", { params: {
				q: searchQueryString,
				c: columnsToRetrieve.join(",") || "*",
				count: count,
				skip: skip,
				orderBy: orderBy,
				orderByDirection: orderByDirection
			}})
			.then(FlexClient.getSearchResults, this.handleError);
		}
		
		public getDocsCount(indexName) {
			return this.$http.get(this.FlexSearchUrl + "/indices/" + indexName + "/documents", { params: {
				count: 1
			}})
			.then(FlexClient.getData)
			.then(result => parseInt(result.TotalAvailable), this.handleError)
		}
		
		public getIndexSize(indexName) {
			return this.$http.get(this.FlexSearchUrl + "/indices/" + indexName + "/size")
			.then(FlexClient.getData)
			.then(result => parseInt(result), this.handleError);
		}
		
		public getMemoryDetails() {
			return this.$http.get(this.FlexSearchUrl + "/memory")
			.then(FlexClient.getData)
			.then(result => <MemoryDetailsResponse>result, this.handleError);
		}
		
		public setupDemoIndex() {
			return this.$http.put(this.FlexSearchUrl + "/setupdemo", {});
		}
		
		public getIndexStatus(indexName) {
			return this.$http.get(this.FlexSearchUrl + "/indices/" + indexName + "/status")
			.then(FlexClient.getData)
			.then(result => <string>result.Status, this.handleError);
		}
		
		public getAnalyzers() {
			return this.$http.get(this.FlexSearchUrl + "/analyzers", {})
			.then(FlexClient.getData)
			.then(result => <Analyzer[]>result, this.handleError);
		}
		
		public testAnalyzer(analyzerName, text) {
			return this.$http.post(this.FlexSearchUrl + "/analyzers/" + analyzerName + "/analyze", {
				"Text": text
			})
			.then(FlexClient.getData)
			.then(result => <string[]>result, this.handleError);
		}
		
		public newPromise(data) {
			return this.$q(function(resolve, reject) {
				resolve(data);
			});
		}
		
		public resolveAllPromises(promises) {
			return this.$q.all(promises);
		}
		
		public indexExists(indexName) {
			return this.$http.get(this.FlexSearchUrl + "/indices/" + indexName + "/exists")
			.then(FlexClient.getData)
			.then(result => <boolean>result.Exists, this.handleError);
		}
		
		public deleteIndex(indexName) {
			return this.$http.delete(this.FlexSearchUrl + "/indices/" + indexName)
			.then(r => r, this.handleError);
		}
	}
}