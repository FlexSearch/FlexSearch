/// <reference path="../../references/references.d.ts" />

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
    constructor($scope: IDemoIndexScope, $state: any, flexClient: FlexClient) {
      var hideProgress = () => $scope.showProgress = false;
      
      // Check if the demo index exists
      flexClient.indexExists("country")
      .then(result => {
        if (result) $scope.hasDemoIndex = true;
        else $scope.hasDemoIndex = false;
      }, () => $scope.hasDemoIndex = false);
      
      $scope.setupDemoIndex = function() {
        $scope.showProgress = true;
        flexClient.setupDemoIndex()
        .then(() => {
          $scope.showProgress = false;
          $scope.hasDemoIndex = true;
        }, hideProgress)
        // Refresh the page
        .then(() => $state.reload());
      };
      
      $scope.deleteDemoIndex = function() {
        $scope.showProgress = true;
        flexClient.deleteIndex("country")
        .then(() => $scope.showProgress = false, hideProgress);
      };
    }
  }
}