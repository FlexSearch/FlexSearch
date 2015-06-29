/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';
  
  export class Index {
    Name: string
    Fields: { Name: string; Value: string; Show: boolean } []
    SearchProfiles: { Name: string; QueryString: string } []
  }
  
  export class Response {
    FieldNames: string[]
    TotalAvailable: number
    RecordsReturned: number
    Documents: string[][]
  }
  
  export interface ISearchBase extends ng.IScope, IMainScope {
    Indices: Index []
    IndexNumber: number
    ActiveIndex: Index
    spQueryString: string
    atLeastOneFieldIsPopulated(): boolean
    submit(index: Index): void
    Response: Response
    DocumentsInPage: string[][]
    showSearchProfileDropDown: boolean
    
    // Pagination specific
    getPage(pageNumber: number): void
    ActivePage: number
    PageCount: number
    PageSize: number
    DupesCount: number
    
    // Progress Bar
    mainProgressBar: boolean
    
    // Settings Bottom Sheet
    $settingsBottomSheet: any
    OrderBy: string
    OrderByDirection: string
    showSettings(event: any): void
    RecordsToRetrieve: number
  }
  
  export class SearchBaseController {
    /* @ngInject */
    constructor($scope: ISearchBase, flexClient: FlexClient, $mdBottomSheet: any) {
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
      // $scope.submit = function(index: Index) { }
      
      // Get the available indices
      flexClient.getIndices()
      .then(response => {
        $scope.mainProgressBar = true;
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
      .then(() => $scope.mainProgressBar = false);
      
      // Function to show settings for search request
      $scope.$settingsBottomSheet = $mdBottomSheet;
      // $scope.showSettings = function($event) { }
    }
  }
}
