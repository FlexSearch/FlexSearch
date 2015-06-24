/// <reference path="..\..\references\references.d.ts" />


module flexportal {
  
  // The processing function supplied by the user
  declare function process(sourceId, targetId, indexName) : ng.IPromise<{}>
  
  class ComparisonItem {
    Name: string
    Id: string
    TrueDuplicate: boolean
    Values: any[]
  }
  
  interface IComparisonScope extends ISessionScope {
    ActiveDuplicate: Duplicate
    FieldNames: string []
    Source: ComparisonItem
    Targets: ComparisonItem []
    areEqual(fieldNumber: number, targetNumber: number): boolean

    // Toolbar specific    
    doProcessing(): void
    doReview(): void
    selectedTarget: string
  }

  export class ComparisonController {
    /* @ngInject */
    constructor($scope: IComparisonScope, $stateParams: any, $http: ng.IHttpService, $mdToast: any, flexClient: FlexClient) {
      // Function to check if two field values from source vs target are equal
      $scope.areEqual = function(fieldNumber, targetNumber) {
        return $scope.Source.Values[fieldNumber] == $scope.Targets[targetNumber].Values[fieldNumber];
      }
      
      // Function that will be executed when the Process button is pressed
      $scope.doProcessing = function() {
        // Show the progress bar
        $('.comparison-page md-progress-linear').show();
        var duplicate = $scope.ActiveDuplicate,
            selectedTargetIdx = parseInt($scope.selectedTarget);
        
        var sourceId = duplicate.SourceRecordId,
            targetId = duplicate.Targets[selectedTargetIdx].TargetRecordId,
            indexName = $scope.session.IndexName;
        
        // Do the user specified processing
        process(sourceId, targetId, indexName)
        .then(function(response) {
          $mdToast.show(
            $mdToast.simple()
              .content(response || "Done!")
              .position("top right")
              .hideDelay(3000));
        })
        // Update the FlexSearch index and the view
        .then(function() {
          
          // Set the status of the Source to Processed
          var dups = $scope.duplicates.filter(d => d.FlexSearchId == duplicate.FlexSearchId);
          if (dups.length > 0) { 
            dups[0].SourceStatus = "2";
            dups[0].SourceStatusName = toSourceStatusName(2);
          }
          duplicate.SourceStatus = "2";
          
          // Set the selected target record to be a True Duplicate
          $scope.Targets[selectedTargetIdx].TrueDuplicate = true;
          duplicate.Targets[selectedTargetIdx].TrueDuplicate = true;
          
          flexClient.updateDuplicate(duplicate)
          .then(function() {
            $mdToast.show(
              $mdToast.simple()
                .content("FlexSearch index updated")
                .position("top right")
                .hideDelay(3000)
            );
          })
          .then(function(){
            // Hide back the progress bar
            $('.comparison-page md-progress-linear').hide();
          });
        });
      };
      
      // Function that will be executed after the Review button is pressed
      $scope.doReview = function() {
        // Show the progress bar
        $('.comparison-page md-progress-linear').show();
        var duplicate = $scope.ActiveDuplicate;
          
        // Get the duplicate from FlexSearch
          
        // Set the status of the Source to Reviewed
        var dups = $scope.duplicates.filter(d => d.FlexSearchId == duplicate.FlexSearchId);
        if (dups.length > 0) { 
          dups[0].SourceStatus = "1";
          dups[0].SourceStatusName = toSourceStatusName(1);
        }
        duplicate.SourceStatus = "1";
        
        flexClient.updateDuplicate(duplicate)
        .then(function() {
          $mdToast.show(
            $mdToast.simple()
              .content("FlexSearch index updated")
              .position("top right")
              .hideDelay(3000)
          );
        })
        .then(function(){
          // Hide back the progress bar
          $('.comparison-page md-progress-linear').hide();
        });
      };
      
      // Get the duplicate that needs to be displayed
      flexClient.getDuplicateBySourceId($stateParams.sessionId, $stateParams.sourceId)
      .then(document => {
        if(document == null) { errorHandler("Couldn't find duplicate"); return; }
        
        // Store the active duplicate
        $scope.ActiveDuplicate = fromDocumentToDuplicate(document);
        
        // Wait until the session properties are received to get the duplicate index name
        // that is needed to get the source and target records
        $scope.sessionPromise
          .then(session => {
            var dupRecs = [$scope.ActiveDuplicate.SourceRecordId] 
              .concat($scope.ActiveDuplicate.Targets.map(t => t.TargetRecordId));
              
            flexClient.getRecordsByIds(session.IndexName, dupRecs) 
            .then(documents => {
              // Populate the FieldNames
              $scope.FieldNames = Object.keys(document.Fields);
              
              // Instantiate the Source Record
              $scope.Source = {
                Name: $scope.ActiveDuplicate.SourceDisplayName, 
                Id: $scope.ActiveDuplicate.SourceId, 
                TrueDuplicate: false,
                Values: []};
              
              var sourceFields = firstOrDefault(documents, "_id", $scope.ActiveDuplicate.SourceRecordId);
              for (var i in sourceFields)
                $scope.Source.Values.push(sourceFields[i]);
              
              // Populate the Targets
              $scope.Targets = [];
              for (var i in $scope.ActiveDuplicate.Targets) {
                var flexTarget = $scope.ActiveDuplicate.Targets[i];
                
                // Instantiate the Target Record
                var target = {
                  Name: flexTarget.TargetDisplayName, 
                  Id: flexTarget.TargetId, 
                  TrueDuplicate: flexTarget.TrueDuplicate,
                  Values: [] };
                
                var targetFields = firstOrDefault(documents, "_id", $scope.ActiveDuplicate.Targets[i].TargetRecordId);
                for (var j in targetFields)
                  target.Values.push(targetFields[j]);
                
                // Add the target to the list of Targets
                $scope.Targets.push(target);
              }
              
              console.log($scope);
            });
            
            // Get the Source record
            flexClient.getRecordById(session.IndexName, $scope.ActiveDuplicate.SourceRecordId)
            .then(document => {
              // Populate the FieldNames
              $scope.FieldNames = Object.keys(document.Fields);
              
              
              
              // Instantiate the Source Record
              $scope.Source = {
                Name: $scope.ActiveDuplicate.SourceDisplayName, 
                Id: $scope.ActiveDuplicate.SourceId, 
                TrueDuplicate: false,
                Values: []};
              
              for (var i in document.Fields)
                $scope.Source.Values.push(document.Fields[i]);
            });
            
            // Get the Targets
            $scope.Targets = [];
            for (var i in $scope.ActiveDuplicate.Targets) {
              (function(flexTarget: FlexSearch.DuplicateDetection.TargetRecord) {
                flexClient.getRecordById(session.IndexName, flexTarget.TargetRecordId)
                .then(document => {
                  // Instantiate the Source Record
                  var target = {
                    Name: flexTarget.TargetDisplayName, 
                    Id: flexTarget.TargetId, 
                    TrueDuplicate: flexTarget.TrueDuplicate,
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
                  $scope.selectedTarget = null;
                  
                  // Check which checkbox changed
                  var cb = $(".ui.checkbox").each(function(i, item){
                    if((<any>$(item)).checkbox('is checked')) {
                      $scope.selectedTarget = $(item).find("input").attr('value');
                    }
                  });
                  
                  $scope.$digest();
                }
              });
            }, 1000);
          });
      });
    }
  }
}
