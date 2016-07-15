/// <reference path="../../common/references/references.d.ts" />
/// <reference path="./indexDetails.ts"/>

module flexportal {
  'use strict';

  import OrderByDirectionEnum = API.Client.SearchQuery.OrderByDirectionEnum
  import SearchQuery = API.Client.SearchQuery

  interface IEditQueryScope extends ng.IScope, IMainScope, IIndexDetailsScope {
    searchQuery : SearchQuery

    directions: string[]
    isNewQuery: boolean
    submit()
  }

  export class EditQueryController {
    /* @ngInject */
    constructor($scope: IEditQueryScope, indicesApi: API.Client.IndicesApi, documentsApi: API.Client.DocumentsApi, analyzerApi: API.Client.AnalyzerApi,
      $q: any, $state: any, $mdToast: any, $timeout: any, $stateParams: any) {

      // Are we editing an existing field or creating a new one?
      $scope.isNewQuery = !$stateParams.queryName;
      $scope.directions = Object.keys(OrderByDirectionEnum).map(k => OrderByDirectionEnum[k]).filter(v => typeof v === "string");
      $scope.searchQuery = <SearchQuery>{};
      $scope.searchQuery.indexName = $scope.index.indexName;

      if ($scope.isNewQuery) {
        // Set default values
        $scope.searchQuery.count = 10;
        $scope.searchQuery.orderByDirection = OrderByDirectionEnum.Ascending;
        $scope.searchQuery.cutOff = 0;
        $scope.searchQuery.skip = 0;
        $scope.searchQuery.returnScore = true;
        $scope.searchQuery.overridePredefinedQueryOptions = false;
        $scope.searchQuery.returnEmptyStringForNull = true;
      }
      else {
        $scope.searchQuery.queryName = $stateParams.queryName;
        var existing = $scope.index.predefinedQueries.filter(q => q.queryName == $scope.searchQuery.queryName)[0];
        $scope.searchQuery.queryName = existing.queryName;
        $scope.searchQuery.columns = existing.columns;
        $scope.searchQuery.count = existing.count;
        $scope.searchQuery.orderBy = existing.orderBy;
        $scope.searchQuery.orderByDirection = existing.orderByDirection;
        $scope.searchQuery.cutOff = existing.cutOff;
        $scope.searchQuery.distinctBy = existing.distinctBy;
        $scope.searchQuery.skip = existing.skip;
        $scope.searchQuery.queryString = existing.queryString;
        $scope.searchQuery.returnScore = existing.returnScore;
        $scope.searchQuery.preSearchScript = existing.preSearchScript;
        $scope.searchQuery.overridePredefinedQueryOptions = existing.overridePredefinedQueryOptions;
        $scope.searchQuery.returnEmptyStringForNull = existing.returnEmptyStringForNull;
        $scope.searchQuery.variables = existing.variables;
        $scope.searchQuery.highlights = existing.highlights;
      }

      $scope.submit = function() {
          $scope.working = true;
          indicesApi.updateIndexPredefinedQueryHandled($scope.searchQuery, $scope.index.indexName)
          .then(r => {
            $mdToast.show(
              $mdToast.simple()
                .content("Predefined query " + $scope.searchQuery.queryName + " processed successfully.")
                .position("top right")
                .hideDelay(3000));
            $scope.working = false;
            $state.go('indexDetails', {}, { reload: true });
          });
      };
    }
  }
}
