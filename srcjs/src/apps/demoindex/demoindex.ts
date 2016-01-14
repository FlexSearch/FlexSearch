/// <reference path="../../common/references/references.d.ts" />

module flexportal {
  'use strict';
  
  interface IDemoIndexScope extends ng.IScope {
    setupDemoIndex() : void
    deleteDemoIndex() : void
    showProgress : boolean
    hasDemoIndex : boolean
  }

  export class DemoIndexController {
    /* @ngInject */
    constructor($scope: IDemoIndexScope, $state: any, serverApi: API.Client.ServerApi, indicesApi: API.Client.IndicesApi) {
      var hideProgress = () => $scope.showProgress = false;
      
      // Check if the demo index exists
      indicesApi.indexExistsHandled("country")
      .then(result => {
        if (result.data.exists) $scope.hasDemoIndex = true;
        else $scope.hasDemoIndex = false;
      }, () => $scope.hasDemoIndex = false);
      
      $scope.setupDemoIndex = function() {
        $scope.showProgress = true;
        serverApi.setupDemoHandled()
        .then(() => {
          $scope.showProgress = false;
          $scope.hasDemoIndex = true;
        }, hideProgress)
        // Refresh the page
        .then(() => $state.reload());
      };
      
      $scope.deleteDemoIndex = function() {
        $scope.showProgress = true;
        indicesApi.deleteIndexHandled("country")
        .then(() => {
            $scope.showProgress = false;
            $scope.hasDemoIndex = false;
        }, hideProgress);
      };
    }
  }
}