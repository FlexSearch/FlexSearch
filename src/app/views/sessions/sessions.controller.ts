/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';

  class Session extends FlexSearch.DuplicateDetection.Session {
    JobStartTimeString: string
    JobEndTimeString: string
  }

  export interface ISessionsScope extends ng.IScope, IMainScope {
    Sessions: Session[]
    ActivePage: number
    PageCount: number
    PageSize: number
    goToSession(sessionId: string): void
    getPage(pageNumber: number): void
  }

  export class SessionsController {
    /* @ngInject */
    constructor($scope: ISessionsScope, $state: any, $http: ng.IHttpService, datePrinter: any, flexClient: FlexClient) {
      $scope.setTitle("");
      
      // Initialize paging
      $scope.PageSize = 10;
      $scope.ActivePage = 1;
      $scope.getPage = function(pageNumber) {
        
        // Set the active page
        if (pageNumber < 1 || pageNumber > $scope.PageCount) return;
        $scope.ActivePage = pageNumber;
        
        // Get the sessions
        flexClient.getSessions(
          $scope.PageSize, 
          ($scope.ActivePage - 1) * $scope.PageSize)
        .then(results => {
          $scope.goToSession = function(sessionId) {
            $state.go('session', {sessionId: sessionId});
          };
  
          $scope.Sessions = results.Documents
            .map(d => <Session>JSON.parse(d.Fields["sessionproperties"]))
            .map(s => {
              s.JobStartTimeString = datePrinter.toDateStr(s.JobStartTime);
              s.JobEndTimeString = datePrinter.toDateStr(s.JobEndTime);
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
