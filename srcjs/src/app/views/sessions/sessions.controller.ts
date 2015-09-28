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
    GridOptions: uiGrid.IGridOptions
    goToSession(sessionId: string): void
    getPage(pageNumber: number): void
    toggleRight(): void
    closeSidenav(): void
  }

  export class SessionsController {
    /* @ngInject */
    constructor($scope: ISessionsScope, $state: any, $http: ng.IHttpService, datePrinter: any, flexClient: FlexClient, $mdSidenav: any, $mdUtil: any) {
      $scope.GridOptions = new DataGrid.GridOptions();
      $scope.GridOptions.columnDefs = [
        new DataGrid.ColumnDef("IndexName"),
        new DataGrid.ColumnDef("ProfileName"),
        new DataGrid.ColumnDef("JobStartTimeString", "Job Start Time"),
        new DataGrid.ColumnDef("JobEndTimeString", "Job End Time"),
        new DataGrid.ColumnDef("RecordsReturned"),
        new DataGrid.ColumnDef("RecordsAvailable")
      ]
      
      // Initialize paging
      $scope.PageSize = 50;
      $scope.ActivePage = 1;
      
      // Get data from the server
      $scope.GridOptions.useExternalPagination = true;
      $scope.GridOptions.paginationCurrentPage = $scope.ActivePage;
      $scope.GridOptions.paginationPageSize = $scope.PageSize;
      $scope.GridOptions.paginationPageSizes = [];
      
      $scope.GridOptions.onRegisterApi = function(gridApi) {
        gridApi.selection.on.rowSelectionChanged($scope, function(row) {
          console.log(row);
          var r = <uiGrid.IGridRow>row;
          $state.go('session', { sessionId: r.entity.SessionId });
        });
        gridApi.pagination.on.paginationChanged($scope, function (newPage, pageSize) {
          $scope.getPage(newPage);
      });
      }
      
      // Handler for toggling the sidenav
      function buildToggler(navID) {
        var debounceFn = $mdUtil.debounce(function() {
          $mdSidenav(navID)
            .toggle();
        }, 300);
        return debounceFn;
      }
      $scope.toggleRight = buildToggler('right');
      
      // Function for closing the sidenav
      $scope.closeSidenav = function() {
        $mdSidenav('right').close();
      }; 
      
      // Helper function for navigating to a different section
      $scope.goToSession = function(sessionId) {
        $state.go('session', { sessionId: sessionId });
      };
      
      // Function for monitoring the sidenav close
      $scope.$watch(
        function() { return $mdSidenav('right').isOpen(); },
        function(newValue, oldValue) {
          if (newValue == false)
            $state.go('sessions');
        });
      
      $scope.getPage = function(pageNumber) {
        // Display progress bar
        var progress = $('.sessions-page md-progress-linear');
        progress.show();
        
        // Set the active page
        if (pageNumber < 1 || (pageNumber > $scope.PageCount && $scope.PageCount > 0)) return;
        $scope.ActivePage = pageNumber;
        
        // Get the sessions
        flexClient.getSessions(
          $scope.PageSize,
          ($scope.ActivePage - 1) * $scope.PageSize,
          "_lastmodified", "desc")
          .then(results => {
            $scope.Sessions = results.Documents
              .map(d => <Session>JSON.parse(d.Fields["sessionproperties"]))
              .map(s => {
                s.JobStartTimeString = datePrinter.toDateStr(s.JobStartTime);
                s.JobEndTimeString = datePrinter.toDateStr(s.JobEndTime);
                return s;
              });

            $scope.GridOptions.data = $scope.Sessions;
            // Set the number of pages
            $scope.PageCount = Math.ceil(results.TotalAvailable / $scope.PageSize);
            // Set the grid total items, otherwise it will display it to be 1
            $scope.GridOptions.totalItems = results.TotalAvailable;
            
            console.debug("Scope Sessions", $scope.Sessions);
            console.debug('Fetched page:' + pageNumber);
            console.debug('Total page count:' + $scope.PageCount);
          })
          .then(() => progress.hide());
      };
      
      // Get the active page
      $scope.getPage($scope.ActivePage);
    }
  }
}
