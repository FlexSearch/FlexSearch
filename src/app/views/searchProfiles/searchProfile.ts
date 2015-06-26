/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';
  
  class Index {
    Name: string
    Fields: { Name: string; Value: string; Show: boolean } []
    SearchProfiles: { Name: string; QueryString: string } []
  }
  
  class Response {
    FieldNames: string[]
    TotalAvailable: number
    RecordsReturned: number
    Documents: string[][]
  }
  
  export interface ISearchProfile extends ng.IScope, IMainScope {
    Indices: Index []
    IndexNumber: number
    ActiveIndex: Index
    RecordsToRetrieve: number
    spQueryString: string
    atLeastOneFieldIsPopulated(): boolean
    submit(index: Index): void
    Response: Response
    DocumentsInPage: string[][]
    showSettings(event: any): void
    
    // Pagination specific
    getPage(pageNumber: number): void
    ActivePage: number
    PageCount: number
    PageSize: number
    DupesCount: number
  }

  export class SearchProfileController {
    /* @ngInject */
    constructor($scope: ISearchProfile, $state: any, flexClient: FlexClient, $mdSidenav: any, $mdUtil: any, $mdBottomSheet: any) {
      // The Progress bars
      var indicesProgress = $("md-progress-linear.indices");
      
      // Set the default value for the Search Profile Query string
      // to serve as an example
      $scope.spQueryString = "firstname = ''";
      
      // Function for telling if there is at least one input on the left
      // navigation that is filled in
      $scope.atLeastOneFieldIsPopulated = function() {
        return $scope.Indices[$scope.IndexNumber].Fields
          .filter(f => f.Value != undefined)
          .length > 0;
      };
      
      // Pagination
      $scope.ActivePage = 1;
      $scope.PageSize = 15;
      $scope.getPage = function(pageNumber) {
        // Set the active page
        if (pageNumber < 1 || pageNumber > $scope.PageCount) return;
        $scope.ActivePage = pageNumber;
        
        if(!$scope.Response) return;
        
        $scope.DocumentsInPage = $scope.Response.Documents
          .slice(($scope.ActivePage - 1) * $scope.PageSize, 
            $scope.ActivePage * $scope.PageSize);
      }
      
      // Function that submits the Search Profile test to FlexSearch
      $scope.submit = function(index: Index) {
        // Build the Search Query String
        var searchQueryString = index.Fields
          .filter(f => f.Value != undefined)
          .map(f => f.Name + ":'" + f.Value + "'")
          .join(",");
          
         // Colate the columns to retrieve
         var columns = index.Fields
            .filter(f => f.Show)
            .map(f => f.Name);
          
         flexClient.submitSearchProfileTest(index.Name, searchQueryString, $scope.spQueryString,
           columns.length == 0 ? undefined : columns, $scope.RecordsToRetrieve)
           .then(result => {
             var r = new Response();
             r.RecordsReturned = result.RecordsReturned;
             r.TotalAvailable = result.TotalAvailable;
             r.FieldNames = [];
             r.Documents = [];
             for (var i in result.Documents) {
               var doc = result.Documents[i];
               // Populate the field names if they're empty
               if (r.FieldNames.length == 0)
                 r.FieldNames = Object.keys(doc.Fields);
                 
               r.Documents.push(doc.Fields);
             }
             
             $scope.Response = r;
           })
           .then(() => {
             $scope.PageCount = Math.ceil($scope.Response.RecordsReturned / $scope.PageSize);
             $scope.getPage(1);
           });
      };
      
      // Get the available indices
      flexClient.getIndices()
      .then(response => {
        indicesProgress.show();
        $scope.Indices = response.map(i => {
          var idx = new Index();
          idx.Name = i.IndexName;
          idx.Fields = i.Fields.map(f => { return {
            Name: f.FieldName,
            Value: undefined,
            Show: false }; 
          });
          idx.SearchProfiles = i.SearchProfiles.map(sp => { 
            return {
              Name: sp.QueryName, 
              QueryString: sp.QueryString } });
          return idx; 
          });
      })
      .then(() => indicesProgress.hide());
      
      // Function to show settings for search request
      $scope.showSettings = function($event){
         $mdBottomSheet.show({
            templateUrl: 'app/views/searchProfiles/searchProfileSettings.html',
            controller: 'SearchProfileSettingsController',
            targetEvent: $event,
            parent: $('.leftColumn'),
            scope: $scope,
            preserveScope: true
          });
      }
    }
  }
}
