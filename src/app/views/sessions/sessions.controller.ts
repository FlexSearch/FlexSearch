/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';

  class Session extends FlexSearch.DuplicateDetection.Session {
    JobStartTimeString: string
    JobEndTimeString: string
  }

  interface ISessionsScope extends ng.IScope {
    Sessions: Session[]
    ActivePage: number
    PageCount: number
    PageSize: number
    goToSession(sessionId: string): void
    getPage(pageNumber: number): void
  }

  export class SessionsController {
    /* @ngInject */
    constructor($scope: ISessionsScope, $state: any, $http: ng.IHttpService) {
      
      // Initialize paging
      $scope.PageSize = 10
      $scope.ActivePage = 1
      $scope.getPage = function(pageNumber) {
        
        // Set the active page
        if (pageNumber < 1 || pageNumber > $scope.PageCount) return;
        $scope.ActivePage = pageNumber;
        
        // Get the sessions
        $http.get(DuplicatesUrl + "/search", { params: {
          c: "*",
          q: "type = 'session'",
          skip: ($scope.ActivePage - 1) * $scope.PageSize,
          count: $scope.PageSize
        }})
        .then((response: any) => {
          $scope.goToSession = function(sessionId) {
            $state.go('session', {sessionId: sessionId});
          };
  
          var toDateStr = function(dateStr: any) {
            var date = new Date(dateStr);
            return date.toLocaleDateString() + ", " + date.toLocaleTimeString();
          }
  
          var results = <FlexSearch.Core.SearchResults>response.data.Data;
          $scope.Sessions = results.Documents
            .map(d => <Session>JSON.parse(d.Fields["sessionproperties"]))
            .map(s => {
              s.JobStartTimeString = toDateStr(s.JobStartTime);
              s.JobEndTimeString = toDateStr(s.JobEndTime);
              return s;
            });
            
          // Set the number of pages
          $scope.PageCount = Math.ceil(results.TotalAvailable / $scope.PageSize);
        });
      };
      
      // Get the active page
      $scope.getPage($scope.ActivePage);
    }
  }
}
