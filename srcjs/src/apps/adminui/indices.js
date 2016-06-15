var flexportal;
(function (flexportal) {
    'use strict';
    var IndicesController = (function () {
        function IndicesController($scope, indicesApi, documentsApi, $q) {
            $scope.prettysize = IndicesController.getPrettySizeFunc();
            $scope.numberPart = function (s) { return s.split(" ")[0]; };
            $scope.symbolPart = function (s) { return s.split(" ")[1]; };
            this.GetIndicesData($scope, indicesApi, documentsApi, $q);
        }
        IndicesController.getPrettySizeFunc = function () {
            var sizes = [
                'B', 'kB', 'MB', 'GB', 'TB', 'PB', 'EB'
            ];
            var prettysize = function (size, nospace, one) {
                var mysize, f;
                sizes.forEach(function (f, id) {
                    if (one) {
                        f = f.slice(0, 1);
                    }
                    var s = Math.pow(1024, id), fixed;
                    if (size >= s) {
                        fixed = String((size / s).toFixed(1));
                        if (fixed.indexOf('.0') === fixed.length - 2) {
                            fixed = fixed.slice(0, -2);
                        }
                        mysize = fixed + (nospace ? '' : ' ') + f;
                    }
                });
                if (!mysize) {
                    f = (one ? sizes[0].slice(0, 1) : sizes[0]);
                    mysize = '0' + (nospace ? '' : ' ') + f;
                }
                return mysize;
            };
            return prettysize;
        };
        IndicesController.prototype.GetIndicesData = function ($scope, indicesApi, documentsApi, $q) {
            $scope.showProgress = true;
            return indicesApi.getAllIndicesHandled()
                .then(function (response) { return $scope.indices = response.data; })
                .then(function () { return $q.all($scope.indices.map(function (i) { return indicesApi.getIndexStatusHandled(i.indexName); })); })
                .then(function (statuses) {
                $scope.indices.forEach(function (idx, i) { return idx.statusReason = statuses[i].data.indexStatus.toString(); });
            })
                .then(function () { return $q.all($scope.indices.map(function (i) { return documentsApi.getDocumentsHandled(i.indexName)
                .then(function (result) { return result.data.totalAvailable; }); })); })
                .then(function (docCounts) { return $scope.indices.forEach(function (idx, i) { return idx.docsCount = docCounts[i]; }); })
                .then(function () { return $q.all($scope.indices.map(function (i) { return indicesApi.getIndexSizeHandled(i.indexName); })); })
                .then(function (sizes) {
                return $scope.indices.forEach(function (idx, i) { return idx.diskSize = sizes[i].data; });
            })
                .then(function () {
                $scope.indices.push($scope.indices[0]);
                $scope.indices.push($scope.indices[0]);
                $scope.indices.push($scope.indices[0]);
                $scope.indices.push($scope.indices[0]);
            })
                .then(function () { $scope.showProgress = false; return; });
        };
        return IndicesController;
    }());
    flexportal.IndicesController = IndicesController;
})(flexportal || (flexportal = {}));
