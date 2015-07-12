/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';

  interface IIndexDetailsScope extends ng.IScope, IClusterScope {
    currentIndex: any
    IndexName: string
  }

  export class IndexDetailsController {
    
    /* @ngInject */
    constructor($scope: IIndexDetailsScope, $stateParams: any) {
      $scope.IndexName = $stateParams.indexName;
      $scope.setTitle("'" + $scope.IndexName + "' index");
      $scope.currentIndex = { Status: "Retrieving data" };


      $scope.IndicesPromise.then(() => {
        var filtered = $scope.Indices.filter(i => i.IndexName == $scope.IndexName);

        if (filtered.length != 1) {
          $scope.currentIndex = { Error: "Couldn't find index " + $scope.IndexName };
          return;
        }

        $scope.currentIndex = filtered[0];
      });
    }
  }
}
