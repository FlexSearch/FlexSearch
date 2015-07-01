/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';

  interface IClusterScope extends ng.IScope, IMainScope {
    ChartsData: { Data: number[]; Labels: string[] } []
    Charts: any[]
    rerender(chart: any, show: boolean): void
    Indices: IndexResult []
  }

  export class ClusterController {
    
    private static GetIndicesData(flexClient: FlexClient, $scope: IClusterScope) {
      flexClient.getIndices()
      .then(response => $scope.Indices = response);
    }
    
    /* @ngInject */
    constructor($scope: IClusterScope, $state: any, $timeout: ng.ITimeoutService, flexClient: FlexClient) {
      // Clear the chart binding data if window is resized
      $(window).resize(function() {
        $scope.Charts.forEach((c, i) => c.destroy());
        $scope.Charts = [];
        $scope.ChartsData = [];
      });
      
      var getChartById = function (chartId) {
        var filtered = $scope.Charts
          .filter(c => c.chart.canvas.id == chartId);
          
        if(filtered.length == 0) {
          $scope.showError("Couldn't find chart with ID " + chartId);
          return null;
        }
        
        return filtered[0];
      }

      $scope.ChartsData = [];
      $scope.Charts = [];
      $scope.$on('create', function(event, data) {
        $scope.Charts.push(data);
      });

      // Get the data for the charts
      ClusterController.GetIndicesData(flexClient, $scope);
      
      var chartDataStore = [];
      chartDataStore['indices'] = {
        Data: [4, 1, 1],
        Labels: ["Online", "Recovering", "Offline"]
      };
      chartDataStore['memory'] = {
        Data: [1, 10],
        Labels: ["Used", "Free"]
      };
      chartDataStore['disk'] = {
        Data: [0.2, 10],
        Labels: ["Used", "Free"]
      };

      $scope.rerender = function(chartName, show) {
        if (show) {
          // Destroy the charts if they exist
          if($scope.ChartsData[chartName] != undefined) {
            $scope.Charts.forEach((c, i) => c.destroy());
            $scope.Charts = [];
            $scope.ChartsData = [];
          }    
          // Rebuild the chart
          $timeout(function() {
            $scope.ChartsData[chartName] = chartDataStore[chartName];
          });
        }
      }
    }
  }
}
