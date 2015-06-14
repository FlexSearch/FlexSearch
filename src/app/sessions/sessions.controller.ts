module flexportal {
  'use strict';
  class Session {
    SessionId: string
    IndexName: string
    ProfileName: string
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
      $http.get("http://localhost:9800/indices/duplicates/search?c=*&returnflatresult=true&q=type+=+'session'").then((response: any) => {
        $scope.Sessions = [];
        $scope.Limit = 1;
        $scope.Page = 5;
        $scope.Total = 5;
        console.log(response.data);
        for (var i = 0; i < response.data.Data.length; i++) {
          var data = response.data.Data[i];
          console.log(data);
          var sessionProperties = JSON.parse(data.sessionproperties);
          var session = new Session();
          session.IndexName = sessionProperties.IndexName;
          session.ProfileName = sessionProperties.ProfileName;
          $scope.Sessions.push(session);
        }
      });
    }
  }
}
