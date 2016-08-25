/// <reference path="../../common/references/references.d.ts" />

module flexportal {
  'use strict';

  import SearchQuery = API.Client.SearchQuery

  export class SearchIndex {
    Name: string
    Fields: { Name: string; Show: boolean }[]
    SearchProfiles: SearchQuery[]
    Variables: Variable[]
  }

  class Variable {
      Name: string
      Value: string
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
    ActivePredefinedQuery: SearchQuery
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
    refreshVars() : void

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
    constructor($scope: ISearchStudioScope, indicesApi: API.Client.IndicesApi, searchApi: API.Client.SearchApi) {
      $scope.Criteria = "normal";

      $scope.refreshVars = () => {
          $scope.ActiveIndex.Variables = [];
          var vPattern = /@([^\(\)\s,]+)/g;
          var match;
          do {
              match = vPattern.exec($scope.SearchQuery);
              if (match && match[1].toUpperCase() != "IGNORE")
                $scope.ActiveIndex.Variables.push({Name: match[1], Value: null});
          } while (match);
          // Distinct items only
          $scope.ActiveIndex.Variables = _.uniq($scope.ActiveIndex.Variables, function(item, x, y){
              return item.Name;
          });
      };

      // Function to update the Search query with the given value. It makes sure
      // that the query comments are appended.
      $scope.updateSearchQuery = value => {
          $scope.SearchQuery = generateQueryComments() + value;
          $scope.ActivePredefinedQuery = $scope.ActiveIndex.SearchProfiles.filter(sp => sp.queryString == value)[0];
          $scope.refreshVars();
      };

      // Function to help in Autocomplete
      var generateQueryComments = function() {
        var queryComments = "-- DO NOT MODIFY THIS LINE _id _lastmodified _score allOf anyOf like fuzzy matchall regex upToWordsApart isBlank exact gt ge lt le ";
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
        return !!(extractSearchQueryFromACE());
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
      indicesApi.getAllIndicesHandled()
      .then(response => {
        $scope.Indices = response.data.map(i => {
          var idx = new SearchIndex();
          idx.Name = i.indexName;
          idx.Fields = i.fields.map(f => { return {
            Name: f.fieldName,
            Value: undefined,
            Show: true };
          });
          idx.SearchProfiles = i.predefinedQueries;
          return idx;
          });
      })
      .then(() => $scope.mainProgressBar = false)
      .then(() => (<any>$('.scrollable, .ui-grid-viewport')).perfectScrollbar());

    // Function that submits the Search test to FlexSearch
      $scope.submit = function(index: SearchIndex) {
        var flexQuery = null;
        $scope.mainProgressBar = true;

        // Search Profile test
        if($scope.ProfileMode) {
            // Build the Search Query String
            var variables : {[key: string] : string} = {};
            index.Variables
                .filter(f => f.Value != undefined && f.Value != "")
                .forEach((value, index) => variables[value.Name] = value.Value);

            // Colate the columns to retrieve
            var columns = index.Fields
                .filter(f => f.Show)
                .map(f => f.Name);

            var sq = {
                indexName: index.Name,
                queryString: extractSearchQueryFromACE() || "_id matchall 'x'",
                variables: variables,
                columns: columns.length == 0 ? undefined : columns,
                count: $scope.RecordsToRetrieve,
                highlights: $scope.ActivePredefinedQuery.highlights
            };

            console.log("SearchQuery: ", sq);

            flexQuery = searchApi.searchHandled(index.Name, sq);
        }
        // Plain Search test
        else {
            // Colate the columns to retrieve
            var columns = index.Fields
                .filter(f => f.Show)
                .map(f => f.Name);
            var query = extractSearchQueryFromACE();
            console.log("Generated Query:", query);

            var sReq : API.Client.SearchQuery = {
                indexName: index.Name,
                queryString: query,
                columns: columns.length == 0 ? undefined : columns,
                count: $scope.RecordsToRetrieve,
                orderBy: $scope.OrderBy,
                orderByDirection: $scope.OrderByDirection && $scope.OrderByDirection.indexOf("esc") > 0 ? API.Client.SearchQuery.OrderByDirectionEnum.Descending : API.Client.SearchQuery.OrderByDirectionEnum.Ascending
            };
            flexQuery = searchApi.searchHandled(index.Name, sReq);
        }
         // Get the response of the query
         flexQuery
           .then((result : API.Client.SearchResponse) => {
             var r = new SearchResponse();
             var fieldNamesPopulated = false;
             r.RecordsReturned = result.data.recordsReturned;
             r.TotalAvailable = result.data.totalAvailable;
             r.FieldNames = [];
             r.Documents = [];

             for (var i in result.data.documents) {
               var doc = result.data.documents[i];
               var values = [];
               for(var name in doc.fields){
                   // Populate the field names if they're empty
                 if(!fieldNamesPopulated) r.FieldNames.push(new DataGrid.ColumnDef(name));
                 values[name] = doc.fields[name];
               }
               fieldNamesPopulated = true;
               r.Documents.push(values);
             }

             $scope.Response = r;
             $scope.GridOptions.columnDefs = r.FieldNames;
             $scope.GridApi.core.notifyDataChange('column');
             $scope.GridOptions.data = r.Documents;
             console.debug("Received data:", r);

             // Update current page
             $scope.PageCount = Math.ceil($scope.Response.RecordsReturned / $scope.PageSize);
             $scope.getPage(1);

             // Close main progress bar
             $scope.mainProgressBar = false;
           }, () => $scope.mainProgressBar = false);
      };
    }
  }
}
