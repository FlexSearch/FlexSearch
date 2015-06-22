/// <reference path="..\..\references\references.d.ts" />

module flexportal {
  class ComparisonItem {
    Name: string
    Id: string
    Values: any[]
  }
  
  interface IComparisonScope extends ISessionScope {
    ActiveDuplicate: Duplicate
    FieldNames: string []
    Source: ComparisonItem
    Targets: ComparisonItem []
    areEqual(fieldNumber: number, targetNumber: number): boolean
  }

  export class ComparisonController {
    /* @ngInject */
    constructor($scope: IComparisonScope, $stateParams: any, $http: ng.IHttpService) {
      // Function to check if two field values from source vs target are equal
      $scope.areEqual = function(fieldNumber, targetNumber) {
        return $scope.Source.Values[fieldNumber] == $scope.Targets[targetNumber].Values[fieldNumber];
      }
      
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
              $scope.Source = {
                Name: $scope.ActiveDuplicate.SourceDisplayName, 
                Id: $scope.ActiveDuplicate.SourceId, 
                Values: []};
              
              for (var i in document.Fields)
                $scope.Source.Values.push(document.Fields[i]);
            });
            
            // Get the Targets
            $scope.Targets = [];
            for (var i in $scope.ActiveDuplicate.Targets) {
              (function(flexTarget) {
                getRecordById(session.IndexName, flexTarget.TargetRecordId, $http)
                .then(response => {
                  var document = <FlexSearch.Core.DocumentDto>response.data.Data;
                  
                  // Instantiate the Source Record
                  var target = {
                    Name: flexTarget.TargetDisplayName, 
                    Id: flexTarget.TargetId, 
                    Values: [] };
                  
                  for (var j in document.Fields)
                    target.Values.push(document.Fields[j]);
                  
                  // Add the target to the list of Targets
                  $scope.Targets.push(target);
                });
              })($scope.ActiveDuplicate.Targets[i]);
            }
            
            // Enable the checkboxes
            // Since the HTML elements haven't loaded yet, I'm waiting for 1 sec.
            // This should be replaced by some sort of onLoad event. TODO
            window.setTimeout(function(){
              (<any>$('.ui.checkbox')).checkbox({
                onChange: function() {
                  // Initialization
                  var selectedTarget = null
                  $('.md-button.process').attr("ng-disabled", "true");
                  
                  // Check which checkbox changed
                  var cb = $(".ui.checkbox").each(function(i, item){
                    if((<any>$(item)).checkbox('is checked')) {
                      selectedTarget = $(item).find("input").attr('value');
                    }
                  });
                  
                  $scope.$emit('selectedTargetChanged', selectedTarget);
                }
              });
            }, 1000);
          });
      });
    }
  }
}
