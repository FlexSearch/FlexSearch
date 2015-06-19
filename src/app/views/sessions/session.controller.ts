/// <reference path="..\..\references\references.d.ts" />

module flexportal {
    'use strict';

    import Session = FlexSearch.DuplicateDetection.Session;
    import TargetRecord = FlexSearch.DuplicateDetection.TargetRecord;
    
    export class Duplicate extends FlexSearch.DuplicateDetection.SourceRecord {
        Targets: TargetRecord []    
        SourceStatusName: any
    }
    
    export interface ISessionScope extends ng.IScope {
        flexDuplicates: Duplicate[]
        session: Session
        duplicates: any[]
        openMatches(dup: Duplicate) : void
        duplicatesPromise: ng.IPromise<Duplicate[]> 
        sessionPromise: ng.IPromise<Session>
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
        constructor($scope: ISessionScope, $stateParams: any, $http: ng.IHttpService, $state: any) {
            var sessionId = $stateParams.sessionId;
            var pageSize = 20;
      
            // Configure what to do when a match card is clicked
            $scope.openMatches = function(dup: Duplicate){
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
                      
                    return $scope.session;
                });
            })(sessionId);
            
            // Get the Duplicates
            (function(sId) {
                // Store the promise on the $scope to make it accessible by child controllers
                $scope.duplicatesPromise = $http.get(DuplicatesUrl + "/search", 
                    { params: { 
                        q: "type = 'source' and sessionid = '" + sId + "'",
                        c: "*",
                        count: pageSize } }
                )
                .then(function(response : any) {
                    var results = <FlexSearch.Core.SearchResults>response.data.Data;
                    
                    $scope.flexDuplicates = results.Documents.map(fromDocumentToDuplicate);
                        
                    return $scope.flexDuplicates;
                });
            })(sessionId);

            $scope.duplicates = [
                {
                    Source: { Name: "Vladimir Negacevschi", Status: "Processed", Icon: "done", Quality: 2 },
                    Targets: [
                        { Name: "Vladimir", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vlad", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vladi", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Seemant Rajvanshi", Status: "Proposed", Icon: "speaker_notes", Quality: 3 },
                    Targets: [
                        { Name: "See", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Raj", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Ant", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Scott Hanselman", Status: "Reviewed", Icon: "flag", Quality: 4 },
                    Targets: [
                        { Name: "Hansel", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Man", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Scooby", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Elliot Fu", Status: "Processed", Icon: "done", Quality: 5 },
                    Targets: [
                        { Name: "Fu", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Elliot", Score: 100, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Helen Troy", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Helen", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Iron Man", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Steel", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Boy", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]

                },
                {
                    Source: { Name: "Vladimir Negacevschi", Status: "Processed", Icon: "done", Quality: 2 },
                    Targets: [
                        { Name: "Vladimir", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vlad", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vladi", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Seemant Rajvanshi", Status: "Proposed", Icon: "speaker_notes", Quality: 3 },
                    Targets: [
                        { Name: "See", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Raj", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Ant", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Scott Hanselman", Status: "Reviewed", Icon: "flag", Quality: 4 },
                    Targets: [
                        { Name: "Hansel", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Man", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Scooby", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Elliot Fu", Status: "Processed", Icon: "done", Quality: 5 },
                    Targets: [
                        { Name: "Fu", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Elliot", Score: 100, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Helen Troy", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Helen", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Iron Man", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Steel", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Boy", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]

                },
                {
                    Source: { Name: "Vladimir Negacevschi", Status: "Processed", Icon: "done", Quality: 2 },
                    Targets: [
                        { Name: "Vladimir", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vlad", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vladi", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Seemant Rajvanshi", Status: "Proposed", Icon: "speaker_notes", Quality: 3 },
                    Targets: [
                        { Name: "See", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Raj", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Ant", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Scott Hanselman", Status: "Reviewed", Icon: "flag", Quality: 4 },
                    Targets: [
                        { Name: "Hansel", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Man", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Scooby", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Elliot Fu", Status: "Processed", Icon: "done", Quality: 5 },
                    Targets: [
                        { Name: "Fu", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Elliot", Score: 100, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Helen Troy", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Helen", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Iron Man", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Steel", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Boy", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]

                },
                {
                    Source: { Name: "Vladimir Negacevschi", Status: "Processed", Icon: "done", Quality: 2 },
                    Targets: [
                        { Name: "Vladimir", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vlad", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vladi", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Seemant Rajvanshi", Status: "Proposed", Icon: "speaker_notes", Quality: 3 },
                    Targets: [
                        { Name: "See", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Raj", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Ant", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Scott Hanselman", Status: "Reviewed", Icon: "flag", Quality: 4 },
                    Targets: [
                        { Name: "Hansel", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Man", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Scooby", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Elliot Fu", Status: "Processed", Icon: "done", Quality: 5 },
                    Targets: [
                        { Name: "Fu", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Elliot", Score: 100, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Helen Troy", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Helen", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Iron Man", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Steel", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Boy", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]

                },
                {
                    Source: { Name: "Vladimir Negacevschi", Status: "Processed", Icon: "done", Quality: 2 },
                    Targets: [
                        { Name: "Vladimir", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vlad", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Vladi", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Seemant Rajvanshi", Status: "Proposed", Icon: "speaker_notes", Quality: 3 },
                    Targets: [
                        { Name: "See", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Raj", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Ant", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Scott Hanselman", Status: "Reviewed", Icon: "flag", Quality: 4 },
                    Targets: [
                        { Name: "Hansel", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Man", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Scooby", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Elliot Fu", Status: "Processed", Icon: "done", Quality: 5 },
                    Targets: [
                        { Name: "Fu", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Elliot", Score: 100, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Helen Troy", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Helen", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]
                },
                {
                    Source: { Name: "Iron Man", Status: "Processed", Icon: "done", Quality: 1 },
                    Targets: [
                        { Name: "Steel", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" },
                        { Name: "Boy", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650" }]

                }

            ];
            var dup = $scope.duplicates;

            $scope.duplicates = dup;//dup.concat(dup,dup,dup);
      
        }
    }
}
