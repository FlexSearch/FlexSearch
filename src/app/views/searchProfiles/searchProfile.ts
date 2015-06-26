/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';
  
  class Index {
    Name: string
    Fields: { Name: string; Value: string; Show: boolean } []
    SearchProfiles: { Name: string; QueryString: string } []
  }
  
  export interface ISearchProfile extends ng.IScope, IMainScope {
    Indices: Index []
    IndexNumber: number
    ActiveIndex: Index
  }

  export class SearchProfileController {
    /* @ngInject */
    constructor($scope: ISearchProfile, $state: any, flexClient: FlexClient, $mdSidenav: any, $mdUtil: any) {
      // The Progress bars
      var indicesProgress = $("md-progress-linear.indices");
      
      // Get the available indices
      flexClient.getIndices()
      .then(response => {
        indicesProgress.show();
        $scope.Indices = response.map(i => {
          var idx = new Index();
          idx.Name = i.IndexName;
          idx.Fields = i.Fields.map(f => { return {
            Name: f.FieldName,
            Value: undefined,
            Show: false }; 
          });
          idx.SearchProfiles = i.SearchProfiles.map(sp => { 
            return {
              Name: sp.QueryName, 
              QueryString: sp.QueryString } });
          return idx; 
          });
      })
      .then(() => indicesProgress.hide());
      
    }
  }
}
