/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';

  interface IClusterScope extends ng.IScope, IMainScope {
    ChartsData: { Data: number[]; Labels: string[] }[]
    Charts: any[]
    rerender(chart: any, show: boolean): void
    Indices: IndexResult[]
    RadarChart: LinearInstance
    BarChart: LinearInstance
    // Shows which small chart is being displayed on the right column
    Rendering: string
  }

  var colors = ["125, 188, 219", "125, 219, 144", "167, 125, 219",
    "219, 125, 175", "232, 112, 84", "182, 169, 31", "48, 71, 229"]

  export class ClusterController {
    private static unusedColors = colors.slice();

    private static getNextColor() {
      if(this.unusedColors.length > 0)
        return this.unusedColors.pop();
        
      return "0,0,0";
    }

    private static populateDocCount($scope: IClusterScope, dsIdx, dataIdx, value) {
      //$scope.RadarData.datasets[dsIdx].data[dataIdx] = value;
      (<any>$scope.RadarChart).datasets[dsIdx].points[dataIdx].value = 100;
      
      $scope.RadarChart.update();
      
      console.log($scope.RadarChart);
    }
    
    private static toPercentage(array : number[]) {
      var max = Math.max.apply(null, array);
      
      return array.map(x => Math.floor(x/max * 100));
    }
    
    private static createChart(type, canvas, chartVar, data, options?) {
      // Create the chart
      switch (type.toLowerCase()) {
        case "radar": 
          chartVar = new Chart((<any>canvas.get(0)).getContext("2d")).Radar(data, options);
          break; 
        case "bar":
          chartVar = new Chart((<any>canvas.get(0)).getContext("2d")).Bar(data, options);
          break;
      }
      
      // Create the legend
      canvas.parent().append(
        '<chart-legend>' + chartVar.generateLegend() + '</chart-legend>');
    }

    private static GetIndicesData(flexClient: FlexClient, $scope: IClusterScope) {
      flexClient.getIndices()
        .then(response => $scope.Indices = response)
        .then(() => console.log($scope.Indices))
        // Get the number of documents in each index
        .then(() => flexClient.resolveAllPromises(
            $scope.Indices.map(i => flexClient.getDocsCount(i.IndexName))))
        // Create the Radar Chart
        .then(docCounts => {
          console.log(docCounts);
          // Compute everything to percentage
          var docs = ClusterController.toPercentage(docCounts);
          var shards = ClusterController.toPercentage(
            $scope.Indices.map(i => parseInt(i.ShardConfiguration.ShardCount)));
          var profiles = ClusterController.toPercentage(
            $scope.Indices.map(i => i.SearchProfiles.length));
          var fields = ClusterController.toPercentage(
            $scope.Indices.map(i => i.Fields.length));
          
          var radarData : LinearChartData = {
            labels: ["Size", "Shards", "Profiles", "Fields", "Docs"],
            datasets: [] }; 
            
          $scope.Indices.forEach((index, i, idxs) => {
            var nextColor = ClusterController.getNextColor();
            
            var ds = {
              label: index.IndexName,
              fillColor: "rgba(" + nextColor + ",0.2)",
              strokeColor: "rgba(" + nextColor + ",1)",
              data: [
                docs[i],  // TODO
                shards[i],
                profiles[i],
                fields[i],
                docs[i],
              ]
            };
            
            radarData.datasets.push(ds);
          })
          
          ClusterController.createChart("radar", $('#overall'), $scope.RadarChart, 
            radarData, { responsive: false });
            
          return docCounts;
        })
        // Create the Bar Chart
        .then(docCounts => {
          var barData : LinearChartData = {
            labels: $scope.Indices.map(i => i.IndexName),
            datasets: [{
                label: "Number of documents",
                fillColor: "rgba(151,187,205,0.5)",
                strokeColor: "rgba(151,187,205,0.8)",
                highlightFill: "rgba(151,187,205,0.75)",
                highlightStroke: "rgba(151,187,205,1)",
                data: docCounts
              }]
          };
          
          ClusterController.createChart("bar", $('#docs'), $scope.BarChart, 
            barData, { responsive: false });
        })
    }
    
    /* @ngInject */
    constructor($scope: IClusterScope, $state: any, $timeout: ng.ITimeoutService, flexClient: FlexClient) {
      $scope.Rendering = null;
      
      // Clear the chart binding data if window is resized
      $(window).resize(function() {
        $scope.Charts.forEach((c, i) => c.destroy());
        $scope.Charts = [];
        $scope.ChartsData = [];
      });

      var getChartById = function(chartId) {
        var filtered = $scope.Charts
          .filter(c => c.chart.canvas.id == chartId);

        if (filtered.length == 0) {
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
      $scope.ChartsData['overall'] = {
        Data: [
          {
            label: "My First dataset",
            fillColor: "rgba(220,220,220,0.2)",
            strokeColor: "rgba(220,220,220,1)",
            pointColor: "rgba(220,220,220,1)",
            pointStrokeColor: "#fff",
            pointHighlightFill: "#fff",
            pointHighlightStroke: "rgba(220,220,220,1)",
            data: [65, 59, 90, 81, 56, 55, 40]
          },
          {
            label: "My Second dataset",
            fillColor: "rgba(151,187,205,0.2)",
            strokeColor: "rgba(151,187,205,1)",
            pointColor: "rgba(151,187,205,1)",
            pointStrokeColor: "#fff",
            pointHighlightFill: "#fff",
            pointHighlightStroke: "rgba(151,187,205,1)",
            data: [28, 48, 40, 19, 108, 27, 100]
          }
        ]
        ,
        
        // Data : [
        //   [65, 59, 90, 81, 56, 55, 40],
        //   [28, 48, 40, 19, 96, 27, 100]
        // ],
        
        // Labels: ["Eating", "Drinking", "Sleeping", "Designing", "Coding", "Cycling", "Running"]
        Labels: [""]
      };

      $scope.rerender = function(chartName, show) {
        if (show) {
          
          
          // Destroy the charts if they exist
          if ($scope.ChartsData[chartName] != undefined) {
            $scope.Charts.forEach((c, i) => c.destroy());
            $scope.Charts = [];
            $scope.ChartsData = [];
          }    
          // Rebuild the chart
          $timeout(function() {
            $scope.ChartsData[chartName] = chartDataStore[chartName];
            $scope.Rendering = chartName;
          });
        }
        else {
          $scope.Rendering = null;
        }
      }
    }
  }
}
