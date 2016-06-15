/// <reference path="../../common/references/references.d.ts" />

module flexportal {
  'use strict';

  export interface Index extends API.Client.Index {
    docsCount: number
    diskSize: number
    statusReason: string
  }

  export interface IIndicesScope extends ng.IScope, IMainScope {
    showProgress: boolean
    indices: Index[]
    toggleRight(): void
    closeSidenav(): void
    prettysize(s, n, o): string
    numberPart(s): string
    symbolPart(s): string
  }

  export class IndicesController {
    /* @ngInject */
    constructor($scope: IIndicesScope,
      indicesApi: API.Client.IndicesApi,
      documentsApi: API.Client.DocumentsApi,
      $q: any,
      $mdSidenav: any,
      $mdUtil: any,
      $state: any,
      $timeout: any) {
      $scope.prettysize = IndicesController.getPrettySizeFunc();
      $scope.numberPart = s => s.split(" ")[0];
      $scope.symbolPart = s => s.split(" ")[1];

      this.GetIndicesData($scope, indicesApi, documentsApi, $q);

      // Handler for toggling the sidenav
      function buildToggler(navID) {
        var debounceFn = $mdUtil.debounce(function() {
          $mdSidenav(navID)
            .toggle();
        }, 300);
        return debounceFn;
      }
      $scope.toggleRight = buildToggler('right');
      $scope.closeSidenav = function() {
        $mdSidenav("right").close();
      };

      // Function for monitoring the sidenav close
      $scope.$watch(
        function() { return $mdSidenav('right').isOpen(); },
        function(newValue, oldValue) {
          if (newValue == false)
            $state.go('admin-indices');
        });
    }

    private static getPrettySizeFunc() {
      var sizes = [
        'B', 'kB', 'MB', 'GB', 'TB', 'PB', 'EB'
      ];

      /**
        Pretty print a size from bytes
      @method pretty
      @param {Number} size The number to pretty print
      @param {Boolean} [nospace=false] Don't print a space
      @param {Boolean} [one=false] Only print one character
      */

      var prettysize = function(size, nospace, one) {
        var mysize, f;

        sizes.forEach(function(f, id) {
          if (one) {
            f = f.slice(0, 1);
          }
          var s = Math.pow(1024, id),
            fixed;
          if (size >= s) {
            fixed = String((size / s).toFixed(1));
            if (fixed.indexOf('.0') === fixed.length - 2) {
              fixed = fixed.slice(0, -2);
            }
            mysize = fixed + (nospace ? '' : ' ') + f;
          }
        });

        // zero handling
        // always prints in Bytes
        if (!mysize) {
          f = (one ? sizes[0].slice(0, 1) : sizes[0]);
          mysize = '0' + (nospace ? '' : ' ') + f;
        }

        return mysize;
      };

      return prettysize;
    }


    private GetIndicesData($scope: IIndicesScope, indicesApi: API.Client.IndicesApi, documentsApi: API.Client.DocumentsApi, $q: any) {
      $scope.showProgress = true;
      return indicesApi.getAllIndicesHandled()
        .then(response => $scope.indices = <Index[]>response.data)

        // Get the status of each index
        .then(() => $q.all(
          $scope.indices.map(i => indicesApi.getIndexStatusHandled(i.indexName))))
        // Store the statuses on each index
        .then(statuses => {
          $scope.indices.forEach((idx, i) => idx.statusReason = (<API.Client.GetStatusResponse>statuses[i]).data.indexStatus.toString());
        })

        // Get the number of documents in each index
        .then(() => $q.all(
          $scope.indices.map(i => documentsApi.getDocumentsHandled(i.indexName)
            .then(result => result.data.totalAvailable))))
        // Store the number of documents for each index
        .then(docCounts => $scope.indices.forEach((idx, i) => idx.docsCount = docCounts[i]))

        // Get the indices disk size
        .then(() => $q.all(
          $scope.indices.map(i => indicesApi.getIndexSizeHandled(i.indexName))))
        // Store the disk size of the indices
        .then((sizes: API.Client.GetIndexSizeResponse[]) =>
          $scope.indices.forEach((idx, i) => idx.diskSize = sizes[i].data))

        .then(() => {
          $scope.indices.push($scope.indices[0]);
          $scope.indices.push($scope.indices[0]);
          $scope.indices.push($scope.indices[0]);
          $scope.indices.push($scope.indices[0]);
        })

        .then(() => { $scope.showProgress = false; return; });
    }
  }
}
