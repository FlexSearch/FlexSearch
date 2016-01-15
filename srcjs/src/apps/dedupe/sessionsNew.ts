/// <reference path="../../common/references/references.d.ts" />

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
    FileName: string
    ThreadCount: number
    MaxRecordsToScan: number
    MaxDupsToReturn: number
    Indices: Index []
    createSession() : void
    clearDependencies(): void
  }

  export class SessionsNewController {
    /* @ngInject */
    constructor($scope: ISessionsNewScope, commonApi: API.Client.CommonApi, indicesApi : API.Client.IndicesApi, $mdToast: any, $state: any) {
      // Display the frame
      $('md-whiteframe.new-session').show();
      
      // Set the default values for the parameters
      $scope.ThreadCount = 1;
      $scope.SelectionQuery = "_id matchall '*'";
      
      // Get the available indices
      indicesApi.getAllIndexHandled()
      .then(response => {
        $scope.Indices = response.data.map(i => {
          var idx = new Index();
          idx.Name = i.indexName;
          idx.Fields = i.fields.map(f => f.fieldName);
          idx.SearchProfiles = i.searchProfiles.map(sp => { 
            return {
              Name: sp.queryName, 
              QueryString: sp.queryString } });
          return idx; 
          });
      });
      
      $scope.createSession = function() {
        var progress = $("form[name='newSession'] md-progress-linear");
        progress.show();
        
        var index = $scope.Indices[$scope.IndexNumber];
        apiHelpers.submitDuplicateDetection (
          index.Name,
          index.SearchProfiles[$scope.ProfileNumber].Name,
          $scope.FieldName,
          $scope.SelectionQuery,
          $scope.FileName,
          commonApi,
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
