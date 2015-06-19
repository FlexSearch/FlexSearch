module flexportal {

  
  interface IComparisonScope extends ISessionScope {
    ActiveDuplicate: Duplicate
    FieldNames: string []
    SourceValues: { Name:any; Values: any[] }
    TargetValues: { Name:any; Values: any[] } []
  }

  export class ComparisonController {
    /* @ngInject */
    constructor($scope: IComparisonScope, $stateParams: any, $http: ng.IHttpService) {
      // Get the duplicate that needs to be displayed
      $http.get(DuplicatesUrl + "/search", {params: {
        q: "type = 'source' and sessionid = '" + $stateParams.sessionId + "' and sourceid = '" + $stateParams.sourceId + "'",
        c: "*"
      }})
      .then((response: any) => {
        var results = <FlexSearch.Core.SearchResults>response.data.Data;
        
        // Get the first response
        if(results.Documents.length != 1) { errorHandler("no results"); return; }

        // Store the active duplicate
        $scope.ActiveDuplicate = fromDocumentToDuplicate(results.Documents[0]);
        
        // Wait until the session properties are received to get the duplicate index name
        // that is needed to get the source and target records
        $scope.sessionPromise
          .then(session => {
            // Get the Source record
            getRecordById(session.IndexName, $scope.ActiveDuplicate.SourceRecordId, $http)
            .then(response => {
              var document = <FlexSearch.Core.DocumentDto>response.data.Data;
              
              // Populate the FieldNames
              $scope.FieldNames = Object.keys(document.Fields);
              
              // Instantiate the Source Record
              $scope.SourceValues = {Name: $scope.ActiveDuplicate.SourceDisplayName, Values: []};
              
              for (var i in document.Fields)
                $scope.SourceValues.Values.push(document.Fields[i]);
              
              console.log ("Comparison scope ", $scope);
            });
            
            // Get the Targets
          });
      });
      
      

      $scope.$on('$viewContentLoaded', function(event) {
        (<any>$('.ui.checkbox')).checkbox();
      });
    }
  }
}
