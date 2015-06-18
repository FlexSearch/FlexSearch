/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';
  
  class Session extends FlexSearch.DuplicateDetection.Session {
    JobStartTimeString: string 
    JobEndTimeString: string 
  }
  
  interface ISessionsProperties extends ng.IScope {
    Sessions: Session[]
    Limit: number
    Page: number
    Total: number
  }
  
  export class SessionsController {
    /* @ngInject */
    constructor($scope: ISessionsProperties, $http: ng.IHttpService) {
      $http.get("http://localhost:9800/indices/duplicates/search?c=*&q=type+=+'session'").then((response: any) => {
        $scope.Sessions = [];
        $scope.Limit = 1;
        $scope.Page = 5;
        $scope.Total = 5;
        console.log(response.data);
        
        var toDateStr = function(dateStr: any){
          var date = new Date(dateStr);
          return date.toLocaleDateString() + ", " + date.toLocaleTimeString();
        }
        
        var results = <FlexSearch.Core.SearchResults>response.data.Data;
        $scope.Sessions = results.Documents
          .map(d => <Session>JSON.parse(d.Fields["sessionproperties"]))
          .map(s => {
            s.JobStartTimeString = toDateStr(s.JobStartTime);
            s.JobEndTimeString = toDateStr(s.JobEndTime);
            return s;
          });
        $scope.Limit = 10;
        $scope.Page = 1;
        $scope.Total = results.TotalAvailable;
        
        // for (var i = 0; i < response.data.Data.length; i++) {
        //   var data = response.data.Data[i];
        //   console.log(data);
        //   var sessionProperties = JSON.parse(data.sessionproperties);
        //   // var session = ;
        //   // session.IndexName = sessionProperties.IndexName;
        //   // session.ProfileName = sessionProperties.ProfileName;
        //   // $scope.Sessions.push(session);
        // }
      });
    }
  }
}
