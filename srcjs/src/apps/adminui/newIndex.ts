/// <reference path="../../common/references/references.d.ts" />
/// <reference path="indices.ts"/>

module flexportal {
  'use strict';

  import Index = API.Client.Index

  interface INewIndexScope extends ng.IScope, IMainScope, IIndicesScope {
    indexName: string,
    create(): void
  }

  export class NewIndexController {
    /* @ngInject */
    constructor($scope: INewIndexScope, indicesApi: API.Client.IndicesApi, documentsApi: API.Client.DocumentsApi,
      $q: any, $state: any, $mdToast: any, $timeout: any) {

      $scope.create = function() {
        // Display progress bar
        var progress = $('form md-progress-linear');
        progress.show();

        // Check if index already exists
        indicesApi.indexExistsHandled($scope.indexName)
          .then(response => {
            if (response.data.exists) {
              $scope.showError("Index " + $scope.indexName + " already exists on the server.");
            } else {
              let index: Index = { indexName: $scope.indexName };
              indicesApi.createIndexHandled(index)
                .then(r => {
                  $mdToast.show(
                    $mdToast.simple()
                      .content("Index " + $scope.indexName + " created.")
                      .position("top right")
                      .hideDelay(3000));
                  progress.hide();
                  $state.go('admin-indices', {}, { reload: true });
                });
            }
          });
      };


    }
  }
}
