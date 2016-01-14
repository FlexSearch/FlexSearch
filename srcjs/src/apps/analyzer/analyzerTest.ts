/// <reference path="../../common/references/references.d.ts" />

module flexportal {
    'use strict';

    import Analyzer = API.Client.Analyzer;
    import analysisRequest = API.Client.AnalysisRequest

    interface IAnalyzerTestScope extends ng.IScope, IMainScope {
        Analyzers: Analyzer[]
        AnalyzerName: string
        TextToAnalyze: string
        Results: string[]
        runTest(): void
        showProgress: boolean
    }

    export class AnalyzerTestController {
        /* @ngInject */
        constructor($scope: IAnalyzerTestScope, analyzerApi: API.Client.AnalyzerApi) {
            // Get the available analyzers from FlexSearch
            analyzerApi.getAllAnalyzer()
                .then(result => $scope.Analyzers = result.data.data);          

            $scope.runTest = function() {
                $scope.showProgress = true;

                var req : analysisRequest = { text : $scope.TextToAnalyze, analyzerName : $scope.AnalyzerName}
                analyzerApi.analyzeText(req, $scope.AnalyzerName)
                    .then(response => {
                        $scope.Results = response.data.data;
                        $scope.showProgress = false;
                    });
            }
        }
    }
}
