/// <reference path="../../common/references/references.d.ts" />
/// <reference path="indices.ts"/>

module flexportal {
  'use strict';

  import Index = API.Client.Index

  interface IIndexDetailsScope extends ng.IScope, IMainScope {
    indexName: string
    deleteIndex(): void
    refreshIndex(): void
  }

  export class IndexDetailsController {
    /* @ngInject */
    constructor($scope: IIndexDetailsScope, indicesApi: API.Client.IndicesApi, documentsApi: API.Client.DocumentsApi,
      $q: any, $state: any, $mdToast: any, $mdDialog: any, $timeout: any, $stateParams: any) {
      $scope.indexName = $stateParams.indexName;

      // ------------
      // Delete Index
      // -------------
      $scope.deleteIndex = function() {
        // confirm that the user wants to delete index
        $mdDialog.show(
          $mdDialog.confirm()
            .title("Delete index?")
            .textContent("Are you sure you want to delete the index? This action cannot be undone.")
            .ok("Yes")
            .cancel("Cancel"))
          .then(() =>
            // Perform the delete
            indicesApi
              .deleteIndexHandled($scope.indexName)
              .then(r => {
                if (r.data) {
                  this.showToast("Index " + $scope.indexName + " deleted successfully.");
                  $state.go("admin-indices");
                }
              })
          );
      };

      // ------------
      // Refresh Index
      // -------------
      $scope.refreshIndex = function() {
        indicesApi
        .refreshIndexHandled($scope.indexName)
        .then(r => {
          if (r.data)
            this.showToast($mdToast, "Index " + $scope.indexName + " refreshed successfully.");
        })
      }
    }

    showToast($mdToast, message) {
      $mdToast.show(
        $mdToast.simple()
          .textContent(message)
          .position("top right")
          .hideDelay(3000));
    }
  }
}
