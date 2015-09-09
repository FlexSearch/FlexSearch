/// <reference path="../../references/references.d.ts" />
module flexportal {
  'use strict';

  export class SearchIndex {
    Name: string
    Fields: { Name: string; Value: string; Show: boolean }[]
    SearchProfiles: { Name: string; QueryString: string }[]
  }

  export class SearchResponse {
    FieldNames: uiGrid.IColumnDef[]
    TotalAvailable: number
    RecordsReturned: number
    Documents: string[][]
  }

  interface ISearchStudioScope extends ng.IScope {
    Indices: SearchIndex[]
    updatePage() : void
    IndexNumber: number
    ActiveIndex: SearchIndex
    spQueryString: string
    submit(index: SearchIndex): void
    validateSubmit(): boolean
    Response: SearchResponse
    DocumentsInPage: string[][]
    showSearchProfileDropDown: boolean
    Criteria : string
    ProfileMode : boolean
    ReturnAllColumns : boolean
    onReturnAllColumnsClick(): void
    updateSearchQuery(value): void    
    
    // Pagination specific
    getPage(pageNumber: number): void
    ActivePage: number
    PageCount: number
    PageSize: number
    
    // Progress Bar
    mainProgressBar: boolean
    
    // Settings Bottom Sheet
    $settingsBottomSheet: any
    OrderBy: string
    OrderByDirection: string
    showSettings(event: any): void
    RecordsToRetrieve: number
    
    // Grid related
    GridOptions: uiGrid.IGridOptions
    FieldNames : uiGrid.IColumnDef[]
    GridApi : uiGrid.IGridApi
    
    // Ace Options
    AceOptions : any
    SearchQuery : string 
  }
  export class SearchStudioController {
    /* @ngInject */
    constructor($scope: ISearchStudioScope, flexClient: FlexClient) {
      $scope.Criteria = "normal";
      
      // Function to update the Search query with the given value. It makes sure
      // that the query comments are appended.
      $scope.updateSearchQuery = value => $scope.SearchQuery = generateQueryComments() + value;
      
      // Function to help in Autocomplete
      var generateQueryComments = function() {
        var queryComments = "-- DO NOT MODIFY THIS LINE _id _lastmodified _score matchall like fuzzy eq match regex ";
        if($scope.ActiveIndex)
          queryComments += $scope.ActiveIndex.Fields.map(f => f.Name).join(' ');
        return queryComments + '\n'; 
      };
      
      // Gets the actual Search Query, skipping the intellisense metadata
      var extractSearchQueryFromACE = function() {
        return $scope.SearchQuery.split('\n').filter(ln => ln.charAt(0) != '-').join(' ');
      }
      
      $scope.onReturnAllColumnsClick = function() {
        $scope.ActiveIndex.Fields.forEach(f => f.Show = !$scope.ReturnAllColumns);
      };
      
      $scope.SearchQuery = generateQueryComments();
      $scope.RecordsToRetrieve = 100;
      
      $scope.GridOptions = new DataGrid.GridOptions();
      $scope.GridOptions.enableSorting = true;
      $scope.GridOptions.columnDefs = $scope.FieldNames;
      $scope.GridOptions.data =[];
      // Get data from the server
      $scope.GridOptions.useExternalPagination = false;
      
      $scope.GridOptions.onRegisterApi = function(gridApi) {
        // Save it in scope for using it to manipulate the grid
        $scope.GridApi = gridApi;
        console.log("Search Result Grid initialized successfully.");
      }
      
      // Setup ace options
      $scope.AceOptions =
        {
          mode : "sql",
          useWrapMode : true,
          showGutter: true,
          firstLineNumber: 0,
          require: ['ace/ext/language_tools'],
          advanced: {
            enableBasicAutocompletion: true,
            enableLiveAutocompletion: true
          },
          onLoad : function(editor) {
            //TODO : Can use custom completer to push auto complete items in future
          }
        };
        
      // Update the whole UI everytime index is changed
      $scope.updatePage = function () {
        if($scope.IndexNumber)
          $scope.ActiveIndex = $scope.Indices[$scope.IndexNumber];
        $scope.SearchQuery = generateQueryComments();
      };
      
      $scope.validateSubmit = function() {
        // At least one input on the left navigation should be filled in
        // during Search Profile Mode
        var c1 = !$scope.ProfileMode || ($scope.ActiveIndex && 
          $scope.ActiveIndex.Fields
          .filter(f => f.Value != undefined)
          .length > 0);
          
        // The Search Query shouldn't be empty during Search Mode
        var c2 = $scope.ProfileMode || !!(extractSearchQueryFromACE());
        
        return c1 && c2;
      };
      
      // Pagination
      $scope.ActivePage = 1;
      $scope.PageSize = 15;
      $scope.getPage = function(pageNumber) {
        // Clear up the returned documents if there are no results
        if($scope.PageCount == 0) $scope.DocumentsInPage = [];
        
        // Set the active page
        if (pageNumber < 1 || pageNumber > $scope.PageCount) return;
        $scope.ActivePage = pageNumber;
        
        if(!$scope.Response) return;
        
        $scope.DocumentsInPage = $scope.Response.Documents
          .slice(($scope.ActivePage - 1) * $scope.PageSize, 
            $scope.ActivePage * $scope.PageSize);
      }
      
      // Get the available indices
      $scope.mainProgressBar = true;
      flexClient.getIndices()
      .then(response => {
        $scope.Indices = response.map(i => {
          var idx = new SearchIndex();
          idx.Name = i.IndexName;
          idx.Fields = i.Fields.map(f => { return {
            Name: f.FieldName,
            Value: undefined,
            Show: true }; 
          });
          idx.SearchProfiles = i.SearchProfiles.map(sp => { 
            return {
              Name: sp.QueryName, 
              QueryString: sp.QueryString } });
          return idx; 
          });
      })
      .then(() => $scope.mainProgressBar = false)
      .then(() => (<any>$('.scrollable, .ui-grid-viewport')).perfectScrollbar());
      console.log($scope);
    
    // Function that submits the Search test to FlexSearch
      $scope.submit = function(index: Index) {
        var flexQuery = null;
        $scope.mainProgressBar = true;
        
        // Search Profile test
        if($scope.ProfileMode) {
          // Build the Search Query String
          var searchQueryString = index.Fields
            .filter(f => f.Value != undefined && f.Value != "")
            .map(f => f.Name + ":'" + f.Value + "'")
            .join(",");
          
         // Colate the columns to retrieve
         var columns = index.Fields
            .filter(f => f.Show)
            .map(f => f.Name);
            
         flexQuery = flexClient.submitSearchProfileTest(index.Name, 
           searchQueryString || "_id matchall 'x'", 
           extractSearchQueryFromACE(),
           columns.length == 0 ? undefined : columns, $scope.RecordsToRetrieve);
        }
        // Plain Search test
        else {
         // Colate the columns to retrieve
         var columns = index.Fields
            .filter(f => f.Show)
            .map(f => f.Name);
         var query = extractSearchQueryFromACE();
         console.log("Generated Query:", query);
         flexQuery = flexClient.submitSearch(index.Name, query,
           columns.length == 0 ? undefined : columns, $scope.RecordsToRetrieve,
           undefined,
           $scope.OrderBy,
           $scope.OrderByDirection);
        }
         
         // Get the response of the query
         flexQuery  
           .then(result => {
             var r = new SearchResponse();
             r.RecordsReturned = result.RecordsReturned;
             r.TotalAvailable = result.TotalAvailable;
             r.FieldNames = [];
             r.Documents = [];
              
             for (var i in result.Documents) {
               var doc = result.Documents[i];
               // Populate the field names if they're empty
               if (r.FieldNames.length == 0)
                 r.FieldNames = Object.keys(doc.Fields).map(key => new DataGrid.ColumnDef(key));
               r.Documents.push(doc.Fields);
             }
             
             $scope.Response = r;
             $scope.GridOptions.columnDefs = r.FieldNames;
             $scope.GridApi.core.notifyDataChange('column');
             $scope.GridOptions.data = r.Documents;
             console.debug("Received data:", r);
           }, () => $scope.mainProgressBar = false)
           .then(() => {
             $scope.PageCount = Math.ceil($scope.Response.RecordsReturned / $scope.PageSize);
             $scope.getPage(1);
           })
           .then(() => $scope.mainProgressBar = false);
      };
    }
  }
}