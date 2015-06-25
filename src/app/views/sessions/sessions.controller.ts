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
    toggleRight(): void
    closeSidenav() : void
  }

  export class SessionsController {
    /* @ngInject */
    constructor($scope: ISessionsScope, $state: any, $http: ng.IHttpService, datePrinter: any, flexClient: FlexClient, $mdSidenav: any, $mdUtil: any) {
      $scope.setTitle("");
      
      // Handler for toggling the sidenav
      function buildToggler(navID) {
        var debounceFn =  $mdUtil.debounce(function(){
              $mdSidenav(navID)
                .toggle();
            },300);
        return debounceFn;
      }
      $scope.toggleRight = buildToggler('right');
      
      // Function for closing the sidenav
      $scope.closeSidenav = function() {
        $mdSidenav('right').close();
      }; 
      
      // Function for monitoring the sidenav close
      $scope.$watch(
        function () { return $mdSidenav('right').isOpen(); },
        function (newValue, oldValue) {
          if (newValue == false)
            $state.go('sessions');
        });
      
      // Initialize paging
      $scope.PageSize = 20;
      $scope.ActivePage = 1;
      $scope.getPage = function(pageNumber) {
        // Display progress bar
        var progress = $('.sessions-page md-progress-linear');
        progress.show();
        
        // Set the active page
        if (pageNumber < 1 || pageNumber > $scope.PageCount) return;
        $scope.ActivePage = pageNumber;
        
        // Get the sessions
        flexClient.getSessions(
          $scope.PageSize, 
          ($scope.ActivePage - 1) * $scope.PageSize,
          "timestamp", "desc" )
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
        })
        .then(() => progress.hide());
      };
      
      // Get the active page
      $scope.getPage($scope.ActivePage);
    }
  }
}
