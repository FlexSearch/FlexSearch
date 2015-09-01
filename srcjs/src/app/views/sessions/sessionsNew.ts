/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';
  
  class Index {
    Name: string
    Fields: string []
    SearchProfiles: { Name: string; QueryString: string } []
  }

  interface ISessionsNewScope extends ng.IScope, ISessionsScope {
	  IndexNumber: number
    ProfileNumber: number
	  FieldName: string
	  SelectionQuery: string
    ThreadCount: number
    MaxRecordsToScan: number
    MaxDupsToReturn: number
    Indices: Index []
    createSession() : void
    clearDependencies(): void
  }

  export class SessionsNewController {
    /* @ngInject */
    constructor($scope: ISessionsNewScope, flexClient: FlexClient, $mdToast: any, $state: any) {
      // Display the frame
      $('md-whiteframe.new-session').show();
      
      // Set the default values for the parameters
      $scope.ThreadCount = 1;
      $scope.SelectionQuery = "_id matchall '*'";
      
      // Get the available indices
      flexClient.getIndices()
      .then(response => {
        $scope.Indices = response.map(i => {
          var idx = new Index();
          idx.Name = i.IndexName;
          idx.Fields = i.Fields.map(f => f.FieldName);
          idx.SearchProfiles = i.SearchProfiles.map(sp => { 
            return {
              Name: sp.QueryName, 
              QueryString: sp.QueryString } });
          return idx; 
          });
      });
      
      $scope.createSession = function() {
        var progress = $("form[name='newSession'] md-progress-linear");
        progress.show();
        
        var index = $scope.Indices[$scope.IndexNumber];
        flexClient.submitDuplicateDetection (
          index.Name,
          index.SearchProfiles[$scope.ProfileNumber].Name,
          $scope.FieldName,
          $scope.SelectionQuery,
          $scope.ThreadCount,
          $scope.MaxRecordsToScan,
          $scope.MaxDupsToReturn
        )
        .then(() => {
          progress.hide();
          
          $mdToast.show(
              $mdToast.simple()
                .content("Duplicate Detection job submitted")
                .position("top right")
                .hideDelay(3000)
            );
          
          $state.go('sessions');
        });
      };
      
      $scope.clearDependencies = function() {
        $scope.ProfileNumber = undefined;
        $scope.FieldName = undefined;
      };
    }
  }
}
