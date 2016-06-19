/// <reference path="../../common/references/references.d.ts" />
/// <reference path="indices.ts"/>

module flexportal {
  'use strict';

  import FieldTypeEnum = API.Client.Field.FieldTypeEnum;

  export interface IIndexDetailsScope extends ng.IScope, IMainScope {
    indexName: string
    index: Index
    fieldTypes: string[]
    working: boolean
    deleteIndex(): void
    refreshIndex(): void
    toggleIndex(): void
    hasFieldsOfType(string): boolean
    toggleRight() : void
    closeSidenav() : void
  }

  export class IndexDetailsController {
    /* @ngInject */
    constructor($scope: IIndexDetailsScope, indicesApi: API.Client.IndicesApi, documentsApi: API.Client.DocumentsApi,
      $q: any, $state: any, $mdToast: any, $mdDialog: any, $timeout: any, $stateParams: any, $mdUtil, $mdSidenav) {
      $scope.indexName = $stateParams.indexName;
      getIndexData(indicesApi, documentsApi, $scope, $q);
      $scope.fieldTypes = Object.keys(FieldTypeEnum).map(k => FieldTypeEnum[k]).filter(v => typeof v === "string");
      $scope.hasFieldsOfType = function(fieldType) {
        if (!$scope.index) return false;
        return $scope.index.fields.filter(f => f.fieldType.toString() == fieldType).length > 0;
      };

      $scope.toggleRight = buildToggler('right', $mdUtil, $mdSidenav);
      $scope.closeSidenav = function() {
        $mdSidenav("right").close();
      };
      // Function for monitoring the sidenav close
      $scope.$watch(
        function() { return $mdSidenav('right').isOpen(); },
        function(newValue, oldValue) {
          if (newValue == false)
            $state.go('indexDetails');
        });

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
                  showToast($mdToast, "Index " + $scope.indexName + " deleted successfully.");
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
              showToast($mdToast, "Index " + $scope.indexName + " refreshed successfully.");
          });
      };

      // ------------
      // Open/Close Index
      // -------------
      $scope.toggleIndex = function() {
        let newStatus = $scope.index.statusReason == "Online" ? "Offline" : "Online";
        indicesApi
          .updateIndexStatusHandled($scope.indexName, newStatus)
          .then(r => {
            if (r.data) {
              $scope.index.statusReason = newStatus;
              showToast($mdToast, "Index " + $scope.indexName + " changed status to " + newStatus);
            }
          });
      };
    }
  }

  function showToast($mdToast: any, message: string) {
    $mdToast.show(
      $mdToast.simple()
        .textContent(message)
        .position("top right")
        .hideDelay(3000));
  }

  function getIndexData(indicesApi: API.Client.IndicesApi, documentsApi: API.Client.DocumentsApi, $scope: IIndexDetailsScope, $q: any) {
    $scope.working = true;
    return indicesApi.getIndexHandled($scope.indexName)
      .then(response => $scope.index = <Index>response.data)

      // Get the status of the index
      .then(() => indicesApi.getIndexStatusHandled($scope.indexName))
      // Store the status
      .then(status => $scope.index.statusReason = status.data.indexStatus.toString())

      // Get the number of documents
      .then(() => {
        if ($scope.index.statusReason == "Online")
          return documentsApi.getDocumentsHandled($scope.indexName)
            .then(result => result.data.totalAvailable);
        else
          return $q(function(resolve, reject) { resolve(-1); });
      })
      // Store the number of documents
      .then(count => $scope.index.docsCount = count)

      // Get the index disk size
      .then(() => indicesApi.getIndexSizeHandled($scope.indexName))
      // Store the disk size of the indices
      .then(size => $scope.index.diskSize = size.data)

      .then(() => (<any>$('.scrollable, .ui-grid-viewport')).perfectScrollbar())
      .then(() => { $scope.working = false; return; });
  }
}
