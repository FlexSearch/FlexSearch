/// <reference path="../../common/references/references.d.ts" />

module flexportal {
  'use strict';

  import Analyzer = FlexSearch.Core.Analyzer;

  interface IAnalyzerTestScope extends ng.IScope, IMainScope {
    Analyzers : Analyzer[]
    AnalyzerName : string
    TextToAnalyze : string
    Results : string[]
    runTest() : void
    showProgress : boolean    
  }

  export class AnalyzerTestController {
    /* @ngInject */
    constructor($scope: IAnalyzerTestScope) {
		  // Get the available analyzers from FlexSearch
    //   flexClient.getAnalyzers()
    //   .then(result => $scope.Analyzers = result);
    //   
    //   $scope.runTest = function() {
    //     $scope.showProgress = true;
    //     
    //     flexClient.testAnalyzer($scope.AnalyzerName, $scope.TextToAnalyze)
    //     .then(response => {
    //       $scope.Results = response;
    //       $scope.showProgress = false;
    //     });
    //   }
    }
  }
}
