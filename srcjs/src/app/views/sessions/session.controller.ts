/// <reference path="..\..\references\references.d.ts" />

module flexportal {
    'use strict';

    import Session = FlexSearch.DuplicateDetection.Session;
    import TargetRecord = FlexSearch.DuplicateDetection.TargetRecord;
    
    export class Duplicate extends FlexSearch.DuplicateDetection.SourceRecord {
        Targets: TargetRecord []    
        SourceStatusName: any
        FlexSearchId: string
    }
    
    export interface ISessionScope extends ng.IScope, IMainScope {
        duplicates: Duplicate[]
        session: Session
        openMatches(dup: Duplicate) : void
        duplicatesPromise: ng.IPromise<Duplicate[]> 
        sessionPromise: ng.IPromise<Session>
        title : string
        selectedDuplicate : string
        
        // Pagination specific
        getPage(pageNumber: number): void
        ActivePage: number
        PageCount: number
        PageSize: number
        DupesCount: number
    }

    interface IRouteParamsService extends angular.ui.IStateParamsService {
        id: string
    }

    export function fromDocumentToDuplicate(d: FlexSearch.Core.DocumentDto) {
        var dup = new Duplicate();
        dup.FlexSearchId = d.Id;
        dup.SessionId = d.Fields["sessionid"];
        dup.SourceId = d.Fields["sourceid"];
        dup.SourceRecordId = d.Fields["sourcerecordid"];
        dup.TotalDupes = d.Fields["totaldupesfound"];
        dup.SourceStatus = d.Fields["sourcestatus"];
        dup.Notes = d.Fields["notes"];
        dup.SourceStatusName = toSourceStatusName(parseInt(dup.SourceStatus));
        dup.SourceDisplayName = d.Fields["sourcedisplayname"];
        dup.Targets = <TargetRecord[]>JSON.parse(d.Fields["targetrecords"]);
        
        return dup;
    }

    export class SessionController {
        /* @ngInject */
        constructor($scope: ISessionScope, $stateParams: any, $http: ng.IHttpService, $state: any, datePrinter: any, flexClient: FlexClient) {
            var sessionId = $stateParams.sessionId;
            $scope.ActivePage = 1;
            $scope.PageSize = 50;
            
            // Configure what to do when a match card is clicked
            $scope.openMatches = function(dup: Duplicate) {
                $scope.selectedDuplicate = dup.FlexSearchId;
                
                // Display the comparison table
                $state.go('comparison', {sessionId: dup.SessionId, sourceId: dup.SourceId});
            };
      
            // Get the Session Properties
           (function(sId) {
                // Store the promise on the $scope to be accessed by child controllers
                $scope.sessionPromise = flexClient.getSessionBySessionId(sId)
                .then(document => {
                    $scope.session = <Session>JSON.parse(document.Fields["sessionproperties"]);
                      
                    // Display the session details on the top toolbar
                    $scope.title = 
                        "Session for " + $scope.session.IndexName 
                        + " using " + $scope.session.ProfileName + " profile"
                        + " started at " + datePrinter.toDateStr($scope.session.JobStartTime)
                    
                    return $scope.session;
                });
            })(sessionId);
            
            $scope.getPage = function (pageNumber) {
                // Show the progress bar
                var progress = $(".duplicate-list md-progress-linear");
                progress.show();
                
                // Set the active page
                if (pageNumber < 1 || pageNumber > $scope.PageCount) return;
                $scope.ActivePage = pageNumber;
                
                // Get the Duplicates
                (function(sId) {
                    // Store the promise on the $scope to make it accessible by child controllers
                    $scope.duplicatesPromise = flexClient.getDuplicatesFromSession(
                        sId,
                        $scope.PageSize,
                        ($scope.ActivePage - 1) * $scope.PageSize,
                        "sourceid"
                    ) 
                    .then(results => {
                        $scope.duplicates = results.Documents.map(fromDocumentToDuplicate);
                        
                        // Set the total number of Duplicates    
                        $scope.DupesCount = results.TotalAvailable;
                        
                        // Set the number of pages
                        $scope.PageCount = Math.ceil(results.TotalAvailable / $scope.PageSize);
                            
                        // Hide the progress bar
                        progress.hide();
                            
                        return $scope.duplicates;
                    })
                    .then(() => (<any>$('.scrollable')).perfectScrollbar());
                })(sessionId);
            };
            
            // Get the first page
            $scope.getPage(1);
        }
    }
}
