/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';
  
  class Index {
    Name: string
    Fields: string []
    SearchProfiles: string []
  }

  interface ISessionsNewScope extends ng.IScope, ISessionsScope {
	  IndexName: string
	  ProfileName: string
	  DisplayFieldName: string
	  SelectionQuery: string
    Indices: any []
  }

  export class SessionsNewController {
    /* @ngInject */
    constructor($scope: ISessionsNewScope, flexClient: FlexClient) {
      $scope.Indices = [
        {Name: "contact"}, 
        {Name: "contactbdm"},
        {Name: "duplicates"} ];
    }
  }
}
