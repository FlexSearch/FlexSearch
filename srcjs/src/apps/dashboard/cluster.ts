/// <reference path="../../common/references/references.d.ts" />
/// <reference path="../../common/partials/main.controller.ts" />

module flexportal {
  'use strict';

  export interface IndexDetailedResult extends API.Client.Index {
    DocCount: number
    DiskSize: number
    StatusReason: string
  }

  export interface IClusterScope extends ng.IScope, IMainScope {
    ChartsData: { Data: number[]; Labels: string[] }[]
    ChartsDataStore: { Data: number[]; Labels: string[] }[]
    Charts: any[]
    Indices: IndexDetailedResult[]
    MemoryDetails: API.Client.MemoryDetails
    RadarChart: LinearInstance
    BarChart: LinearInstance
    IndicesPromise: ng.IPromise<void>
    FlexSearchUrl : string
    
    // Shows which small chart is being displayed on the right column
    Rendering: string
    
    // Goes to the details page of the given index
    showDetails(indexName): void
    
    // Helper function to pretty print byte values
    prettysize(s,n,o): string
    // Helper function that sums up an array of numbers
    sum(arr: number[]): number
    
    rerender(chart: any, show: boolean): void
    
    // Demo Index related
    setupDemoIndex() : void
    hasDemoIndex: boolean
    
    // Progress bar
    showProgress: boolean
  }

  var colors = ["125, 188, 219", "125, 219, 144", "167, 125, 219",
    "219, 125, 175", "232, 112, 84", "182, 169, 31", "48, 71, 229"]

  export class ClusterController {
    private indicesApi : API.Client.IndicesApi;
    private serverApi : API.Client.ServerApi;
    private documentsApi : API.Client.DocumentsApi;
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
    }
    
    private static toPercentage(array : number[]) {
      var max = Math.max.apply(null, array);
      if(max == 0) return array;
      
      return array.map(x => Math.floor(x/max * 100));
    }
    
    private static createChart(type, canvas, chartVar, data, options?) {
      if (data.datasets.length == 0) return;
      
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

    private getDocsCount(indexName) {
        return this.documentsApi.getDocumentsHandled(indexName)
        .then(result => result.data.totalAvailable, this.documentsApi.handleError)
    }

    private GetIndicesData($scope: IClusterScope, $q: any) {
      $scope.showProgress = true;
      return this.indicesApi.getAllIndicesHandled()
        .then(response => $scope.Indices = <IndexDetailedResult[]>response.data)
        
        // Display the pretty scrollbar for the list of indices
        .then(() => (<any>$('.scrollable')).perfectScrollbar())
        
        // Get the status of each index
        .then(() => $q.all(
            $scope.Indices.map(i => this.indicesApi.getIndexStatusHandled(i.indexName))))
        // Store the indexes on the main index
        .then(statuses => {
          $scope.Indices.forEach((idx, i) => idx.StatusReason = (<API.Client.GetStatusResponse>statuses[i]).data.indexStatus.toString());
          var grouped = _.groupBy($scope.Indices, s => s.StatusReason);
          $scope.ChartsDataStore['indices'] = {
            Data: _.map(grouped, g => g.length),
            Labels: Object.keys(grouped)
          };
        })
        
        // Get the number of documents in each index
        .then(() => $q.all(
            $scope.Indices.map(i => this.documentsApi.getDocumentsHandled(i.indexName)
                                        .then(result => result.data.totalAvailable))))
        // Store the number of documents on the main Index Store
        .then(docCounts => $scope.Indices.forEach((idx, i) => idx.DocCount = docCounts[i]))
        
        // Get the indices disk size
        .then(() => $q.all(
          $scope.Indices.map(i => this.indicesApi.getIndexSizeHandled(i.indexName))))
        // Store the disk size of the indices
        .then((sizes : API.Client.GetIndexSizeResponse[]) => {
          $scope.Indices.forEach((idx, i) => idx.DiskSize = sizes[i].data);
          $scope.ChartsDataStore['disk'] = {
            Data: $scope.Indices.map(i => i.DiskSize),
            Labels: $scope.Indices.map(i => i.indexName)
          };
        })
        
        // Get the memory details
        .then(() => this.serverApi.getServerMemoryDetailsHandled())
        // Store the memory details
        .then(mem => {
          $scope.MemoryDetails = mem.data;
          $scope.ChartsDataStore['memory'] = {
            Data: [mem.data.usedMemory, mem.data.totalMemory - mem.data.usedMemory],
            Labels: ["Used", "Free"] };
        })
        
        // Create the Radar Chart
        .then(() => {
          // Compute everything to percentage
          var sizes = ClusterController.toPercentage(
            $scope.Indices.map(i => i.DiskSize));
          var docs = ClusterController.toPercentage(
            $scope.Indices.map(i => i.DocCount));
          var shards = ClusterController.toPercentage(
            $scope.Indices.map(i => i.shardConfiguration.shardCount));
          var profiles = ClusterController.toPercentage(
            $scope.Indices.map(i => i.predefinedQueries.length));
          var fields = ClusterController.toPercentage(
            $scope.Indices.map(i => i.fields.length));
          
          var radarData : LinearChartData = {
            labels: ["Size", "Shards", "Profiles", "Fields", "Docs"],
            datasets: [] }; 
            
          $scope.Indices.forEach((index, i, idxs) => {
            var nextColor = ClusterController.getNextColor();
            
            var ds = {
              label: index.indexName,
              fillColor: "rgba(" + nextColor + ",0.2)",
              strokeColor: "rgba(" + nextColor + ",1)",
              data: [
                sizes[i],
                shards[i],
                profiles[i],
                fields[i],
                docs[i],
              ]
            };
            
            radarData.datasets.push(ds);
          })
          
          console.log("Radar data:", radarData);
          
          ClusterController.createChart("radar", $('#overall'), $scope.RadarChart, 
            radarData, { responsive: false });
        })
        // Create the Bar Chart
        .then(() => {
          var barData : LinearChartData = {
            labels: $scope.Indices.map(i => i.indexName),
            datasets: [{
                label: "Number of documents",
                fillColor: "rgba(151,187,205,0.5)",
                strokeColor: "rgba(151,187,205,0.8)",
                highlightFill: "rgba(151,187,205,0.75)",
                highlightStroke: "rgba(151,187,205,1)",
                data: $scope.Indices.map(i => i.DocCount)
              }]
          };
          
          ClusterController.createChart("bar", $('#docs'), $scope.BarChart, 
            barData, { responsive: false });
        })
        .then(() => { $scope.showProgress = false; return; });
    }
    
    /* @ngInject */
    constructor($scope: IClusterScope, $state: any, $timeout: ng.ITimeoutService, serverApi: API.Client.ServerApi, documentsApi : API.Client.DocumentsApi, indicesApi : API.Client.IndicesApi, $q: any) {
        this.documentsApi = documentsApi;
        this.serverApi = serverApi;
        this.indicesApi = indicesApi;
      $scope.Rendering = null;
      $scope.FlexSearchUrl = serverApi.basePath;
      $scope.prettysize = ClusterController.getPrettySizeFunc();
      // First assume we have the demo index set up
      $scope.hasDemoIndex = true; 
      
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
      $scope.IndicesPromise = this.GetIndicesData($scope, $q);
      
      // Check if we have a demo index or not
      $scope.IndicesPromise.then(() => $scope.hasDemoIndex = $scope.Indices.some(i => i.indexName == 'country'));

      $scope.ChartsDataStore = [];
      $scope.ChartsDataStore['indices'] = {
        Data: [4, 1, 1],
        Labels: ["Online", "Recovering", "Offline"]
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
            $scope.ChartsData[chartName] = $scope.ChartsDataStore[chartName];
            $scope.Rendering = chartName;
          });
        }
        else {
          $scope.Rendering = null;
        }
      }
      
      $scope.showDetails = function (indexName) {
        $state.go("indexDetails", {indexName: indexName});
      }
      
      $scope.setupDemoIndex = function() {
        $scope.showProgress = true;
        serverApi.setupDemoHandled()
        .then(() => $scope.hasDemoIndex = true)
        .then(() => $scope.showProgress = false)
        // Refresh the page
        .then(() => $state.reload());
      }
      
      $scope.sum = function(arr) {
        if(arr) return arr.reduce((acc, val) => acc + val, 0);
        return 0; 
      };
    }
  }
}
