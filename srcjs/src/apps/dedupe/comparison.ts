/// <reference path="..\..\common\references\references.d.ts" />
/// <reference path="..\..\common\partials\main.controller.ts" />
/// <reference path="session.controller.ts" />
/// <reference path="index.ts" />

module flexportal {
  
  // The processing function supplied by the user
  declare function process(sourceId, targetId, indexName) : ng.IPromise<{}>
  
  // The function that handles the click event of the Source / Match
  declare function onMatchItemClick(item, indexName) : ng.IPromise<{}>
  
  class ComparisonItem {
    Name: string
    Id: string
    ExternalId: string
    TrueDuplicate: boolean
    Values: any[]
  }
  
  interface IComparisonScope extends ISessionScope, IMainScope {
    ActiveDuplicate: apiHelpers.Duplicate
    FieldNames: string []
    Source: ComparisonItem
    Targets: ComparisonItem []
    areEqual(fieldNumber: number, targetNumber: number): boolean
    syncSwitches(masterTargetId) : void
    atLeastOneMaster() : boolean

    // Toolbar specific    
    doProcessing(): void
    doReview(): void
    doItemClick(itemId) : void
  }

  export class ComparisonController {
    private static getTrueDuplicate($scope : IComparisonScope) {
      var filtered = $scope.Targets.filter(t => t.TrueDuplicate);
      if (filtered.length == 1) return filtered[0];
      else $scope.showError("A single true duplicate hasn't been selected");
      
      return null;
    }
    
    private static keyValuePair(fieldNames : string[], comparisonRecord : ComparisonItem){
      var result = [];
      fieldNames.forEach((t, i) => result[t] = comparisonRecord.Values[i]);
      return result;
    }
    
    /* @ngInject */
    constructor($scope: IComparisonScope, $stateParams: any, $http: ng.IHttpService, $mdToast: any, indicesApi: API.Client.IndicesApi, commonApi : API.Client.CommonApi) {
      // Function to check if two field values from source vs target are equal
      $scope.areEqual = function(fieldNumber, targetNumber) {
        return $scope.Source.Values[fieldNumber] == $scope.Targets[targetNumber].Values[fieldNumber];
      }
      
      $scope.atLeastOneMaster = () => $scope.Targets && $scope.Targets.some((t,i) => t.TrueDuplicate);
      
      // Function that synchronizez all the switches within the same group. MasterTargetId is the 
      // target that's currently active
      $scope.syncSwitches = function(masterTargetId) {
        // All targets except the master aren't true duplicates
        $scope.Targets
          .filter(t => t.Id != masterTargetId)
          .forEach((target, i) => target.TrueDuplicate = false);
      };
      
      // Function that will be executed whenever a comparison item header is clicked
      $scope.doItemClick = function(item : ComparisonItem) { 
        onMatchItemClick(ComparisonController.keyValuePair($scope.FieldNames, item), $scope.session.IndexName); 
        }
      
      // Function that will be executed when the Process button is pressed
      $scope.doProcessing = function() {
        // Show the progress bar
        $('.comparison-page md-progress-linear').show();
        
        var duplicate = $scope.ActiveDuplicate,
          masterTarget = ComparisonController.getTrueDuplicate($scope);
        if(masterTarget == null) return;
        
        var source = ComparisonController.keyValuePair($scope.FieldNames, $scope.Source),
            target = ComparisonController.keyValuePair($scope.FieldNames, masterTarget),
            indexName = $scope.session.IndexName;
        
        // Do the user specified processing
        process(source, target, indexName)
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
          duplicate.Targets.forEach((t, i) => { 
            if(t.TargetId == masterTarget.Id) t.TrueDuplicate = true;
          });
          
          apiHelpers.updateDuplicate(duplicate, commonApi)
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
        
        apiHelpers.updateDuplicate(duplicate, commonApi)
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
      apiHelpers.getDuplicateBySourceId($stateParams.sessionId, $stateParams.sourceId, commonApi)
      .then(document => {
        if(document == null) { $scope.showError("Couldn't find duplicate"); return; }
        
        // Store the active duplicate
        $scope.ActiveDuplicate = fromDocumentToDuplicate(document);
        
        // Wait until the session properties are received to get the duplicate index name
        // that is needed to get the source and target records
        $scope.sessionPromise
          .then(session => {
            var dupRecs = [$scope.ActiveDuplicate.SourceRecordId] 
              .concat($scope.ActiveDuplicate.Targets.map(t => t.TargetRecordId));
              
            apiHelpers.getRecordsByIds(session.IndexName, dupRecs, commonApi) 
            .then(documents => {
              // Populate the FieldNames
              $scope.FieldNames = Object.keys(documents[0]);
              
              // Instantiate the Source Record
              $scope.Source = {
                Name: $scope.ActiveDuplicate.SourceDisplayName, 
                Id: $scope.ActiveDuplicate.SourceId,
                ExternalId: $scope.ActiveDuplicate.SourceRecordId, 
                TrueDuplicate: false,
                Values: []};
              
              // If this is a source record that came from a CSV dedupe session, then the contents
              // will be available in the sourcecontent field
              if(!!$scope.ActiveDuplicate.SourceContent) {
                var contents = JSON.parse($scope.ActiveDuplicate.SourceContent);
                $scope.Source.ExternalId = contents.id;
                $scope.FieldNames = Object.keys(contents);
                for (var i in $scope.FieldNames)
                  $scope.Source.Values.push(contents[$scope.FieldNames[i]]);
              }
              else {
                var sourceFields = firstOrDefault(documents, "_id", $scope.ActiveDuplicate.SourceRecordId);
                for (var i in $scope.FieldNames)
                  $scope.Source.Values.push(sourceFields[$scope.FieldNames[i]]);
              }
              
              // Populate the Targets
              $scope.Targets = [];
              for (var i in $scope.ActiveDuplicate.Targets) {
                var flexTarget = $scope.ActiveDuplicate.Targets[i];
                
                // Instantiate the Target Record
                var target = {
                  Name: flexTarget.TargetDisplayName, 
                  Id: flexTarget.TargetId, 
                  ExternalId: flexTarget.TargetRecordId,
                  TrueDuplicate: flexTarget.TrueDuplicate,
                  Values: [] };
                
                var targetFields = firstOrDefault(documents, "_id", $scope.ActiveDuplicate.Targets[i].TargetRecordId);
                for (var j in $scope.FieldNames)
                  target.Values.push(targetFields[$scope.FieldNames[j]]);
                
                // Add the target to the list of Targets
                $scope.Targets.push(target);
              }
              
              console.log($scope);
            })
            .then(() => (<any>$('.scrollable')).perfectScrollbar());
          });
      });
    }
  }
}
