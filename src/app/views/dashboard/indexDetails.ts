/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';

  interface IIndexDetailsScope extends ng.IScope, IClusterScope {
    currentIndex: any
  }

  export class IndexDetailsController {
    
    /* @ngInject */
    constructor($scope: IIndexDetailsScope, $stateParams: any) {
      var indexName = $stateParams.indexName;
      $scope.setTitle("'" + indexName + "' index");
      $scope.currentIndex = { Status: "Retrieving data" };


      $scope.IndicesDataPromise.then(() => {
        var filtered = $scope.Indices.filter(i => i.IndexName == indexName);

        if (filtered.length != 1) {
          $scope.currentIndex = { Error: "Couldn't find index " + indexName };
          return;
        }

        $scope.currentIndex = filtered[0];
      });
    }
  }
}
