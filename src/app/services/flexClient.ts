/// <reference path="..\references\references.d.ts" />

module flexportal {
	
	import SearchResults = FlexSearch.Core.SearchResults;
	import FieldDto = FlexSearch.Core.FieldDto;
	import IndexConfigurationDto = FlexSearch.Core.IndexConfigurationDto;
	
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
		
        /* @ngInject */
		constructor($http: ng.IHttpService, $mdBottomSheet: any, $q: any) {
			 this.$http = $http;
			 this.$mdBottomSheet = $mdBottomSheet;
			 this.$q = $q;
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
			var url = FlexSearchUrl + "/indices/" + indexName + "/documents/" + id + "?c=*";
			return this.$http.get(url)
				.then(
					response => <FlexSearch.Core.DocumentDto>(<any>response.data).Data,
					this.handleError);
	  	}
		  
	  	public getRecordsByIds(indexName, ids: any []) {
			var idStr = ids
				.reduce(function (acc, val) { return acc + ",'" + val + "'" }, "")
				.substr(1);
		  	return this.$http.get(FlexSearchUrl + "/indices/" + indexName + "/search", { params: 
				  {
					 q: "_id eq [" + idStr + "] {clausetype: 'or'}",
					 c: "*",
					 returnFlatResult: "true"
				  } })
			  .then(FlexClient.getFlatResults);
	  	}
		  
	  	// Get the duplicate that needs to be displayed
		public getDuplicateBySourceId(sessionId, sourceId) {
			return this.$http.get(DuplicatesUrl + "/search", {params: {
				q: "type = 'source' and sessionid = '" + sessionId + "' and sourceid = '" + sourceId + "'",		
				c: "*" }}
			)
			.then(FlexClient.getSearchResults, this.handleError)
	      	.then(this.getFirstResponse, this.handleError);
	  	}
		  
	  	public updateDuplicate(duplicate: Duplicate) {
			return this.$http.put(DuplicatesUrl + "/documents/" + duplicate.FlexSearchId,
			{
				Fields: {
				  sessionid: duplicate.SessionId,
				  sourcedisplayname: duplicate.SourceDisplayName,
				  sourceid: duplicate.SourceId,
				  sourcerecordid: duplicate.SourceRecordId,
				  totaldupesfound: duplicate.TotalDupes,
				  type: "source",
				  sourcestatus: duplicate.SourceStatus,
				  targetrecords: JSON.stringify(duplicate.Targets)
				},
				Id: duplicate.FlexSearchId,
				IndexName: "duplicates"
			});
	  	}
		  
		public getSessionBySessionId(sessionId) {
		  return this.$http.get(DuplicatesUrl + "/search", 
		        { params: { 
		            q: "type = 'session' and sessionid = '" + sessionId + "'",
		            c: "*" } }
		    )
		    .then(FlexClient.getSearchResults, this.handleError)
			.then(this.getFirstResponse, this.handleError);
		} 
		
		public getDuplicatesFromSession(sessionId, count, skip, sortby?, sortDirection?) {
            return this.$http.get(DuplicatesUrl + "/search", 
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
			
			return this.$http.get(DuplicatesUrl + "/search", { params: {
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
			
			
			return this.$http.get(FlexSearchUrl + "/indices")
				.then(FlexClient.getData, this.handleError)
				.then(result => <IndexResult []> result, this.handleError);
		}
		
		public submitDuplicateDetection(indexName, searchProfile, displayFieldName, selectionQuery,
			threadCount?, maxRecordsToScan?, maxDupsToReturn?) {
			return this.$http.post(FlexSearchUrl + "/indices/" + indexName + "/duplicatedetection/" + searchProfile, {
					DisplayName: displayFieldName,
					SelectionQuery: selectionQuery,
					ThreadCount: threadCount,
					MaxRecordsToScan: maxRecordsToScan,
					DuplicatesCount: maxDupsToReturn
				});
		}
		
		public submitSearchProfileTest(indexName, searchQueryString, searchProfileQueryString, 
			columnsToRetrieve?: string[], count?, skip?) {
			return this.$http.post(FlexSearchUrl + "/indices/" + indexName + "/searchprofiletest", {
				SearchQuery: {
					QueryString: searchQueryString,
					Columns: columnsToRetrieve || ["*"],
					Count: count,
					Skip: skip
				},
				SearchProfile: searchProfileQueryString
			})
			.then(FlexClient.getData, this.handleError);
		}
		
		public submitSearch(indexName, searchQueryString, columnsToRetrieve?: string[], count?, skip?,
			orderBy?, orderByDirection?) {
			return this.$http.get(FlexSearchUrl + "/indices/" + indexName + "/search", { params: {
				q: searchQueryString,
				c: columnsToRetrieve || ["*"],
				count: count,
				skip: skip,
				orderBy: orderBy,
				orderByDirection: orderByDirection
			}})
			.then(FlexClient.getSearchResults, this.handleError);
		}
	}
}