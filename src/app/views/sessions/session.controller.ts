/// <reference path="..\..\references\references.d.ts" />

module flexportal {
    'use strict';

    import Session = FlexSearch.DuplicateDetection.Session;
    import TargetRecord = FlexSearch.DuplicateDetection.TargetRecord;
    
    export class Duplicate extends FlexSearch.DuplicateDetection.SourceRecord {
        Targets: TargetRecord []    
        SourceStatusName: any
    }
    
    export interface ISessionScope extends ng.IScope, IMainScope {
        duplicates: Duplicate[]
        session: Session
        openMatches(dup: Duplicate) : void
        duplicatesPromise: ng.IPromise<Duplicate[]> 
        sessionPromise: ng.IPromise<Session>
        selectedTarget: string
        
        // Pagination specific
        getPage(pageNumber: number): void
        ActivePage: number
        PageCount: number
        PageSize: number
    }

    interface IRouteParamsService extends angular.ui.IStateParamsService {
        id: string
    }

    export function fromDocumentToDuplicate(d: FlexSearch.Core.DocumentDto) {
        var dup = new Duplicate();
        dup.SessionId = d.Fields["sessionid"];
        dup.SourceId = d.Fields["sourceid"];
        dup.SourceRecordId = d.Fields["sourcerecordid"];
        dup.TotalDupes = d.Fields["totaldupesfound"];
        dup.SourceStatus = d.Fields["sourcestatus"];
        dup.SourceStatusName = toSourceStatusName(parseInt(dup.SourceStatus));
        dup.SourceDisplayName = d.Fields["sourcedisplayname"];
        dup.Targets = <TargetRecord[]>JSON.parse(d.Fields["targetrecords"]);
        
        return dup;
    }

    export class SessionController {
        /* @ngInject */
        constructor($scope: ISessionScope, $stateParams: any, $http: ng.IHttpService, $state: any, datePrinter: any) {
            $scope.selectedTarget = null;
            $scope.$on('selectedTargetChanged', function(event, newValue) {
               $scope.selectedTarget = newValue; 
            });
            var sessionId = $stateParams.sessionId;
            $scope.ActivePage = 1;
            $scope.PageSize = 20;
            
            // Configure what to do when a match card is clicked
            $scope.openMatches = function(dup: Duplicate) {
                // Set the subheader for the list to show the clicked item
                (<any>$(".pagination .md-subheader-content")).html("<div class='activeDuplicate'>" + dup.SourceDisplayName + " (" + dup.SourceId + ")</div>");
                (<any>$(".md-subheader.pagination").show());
                
                // Display the comparison table
                $state.go('comparison', {sessionId: dup.SessionId, sourceId: dup.SourceId});
            };
      
            // Get the Session Properties
           (function(sId) {
                // Store the promise on the $scope to be accessed by child controllers
                $scope.sessionPromise = $http.get(DuplicatesUrl + "/search", 
                    { params: { 
                        q: "type = 'session' and sessionid = '" + sId + "'",
                        c: "*" } }
                )
                .then(function(response : any) {
                    var results = <FlexSearch.Core.SearchResults>response.data.Data;
                    
                    $scope.session = results.Documents
                      .map(d => <Session>JSON.parse(d.Fields["sessionproperties"]))
                      [0];
                      
                    // Display the session details on the top toolbar
                    var title = 
                        "Session for " + $scope.session.IndexName 
                        + " using " + $scope.session.ProfileName + " profile"
                        + " started at " + datePrinter.toDateStr($scope.session.JobStartTime)
                    $scope.setTitle(title);
                  
                    return $scope.session;
                });
            })(sessionId);
            
            $scope.getPage = function (pageNumber) {
                
                // Set the active page
                if (pageNumber < 1 || pageNumber > $scope.PageCount) return;
                $scope.ActivePage = pageNumber;
                
                // Get the Duplicates
                (function(sId) {
                    // Store the promise on the $scope to make it accessible by child controllers
                    $scope.duplicatesPromise = $http.get(DuplicatesUrl + "/search", 
                        { params: { 
                            q: "type = 'source' and sessionid = '" + sId + "'",
                            c: "*",
                            count: $scope.PageSize,
                            skip: ($scope.ActivePage - 1) * $scope.PageSize } }
                    )
                    .then(function(response : any) {
                        var results = <FlexSearch.Core.SearchResults>response.data.Data;
                        
                        $scope.duplicates = results.Documents.map(fromDocumentToDuplicate);
                            
                        // Set the number of pages
                        $scope.PageCount = Math.ceil(results.TotalAvailable / $scope.PageSize);
                            
                        return $scope.duplicates;
                    });
                })(sessionId);
            };
            
            // Get the first page
            $scope.getPage(1);
        }
    }
}
