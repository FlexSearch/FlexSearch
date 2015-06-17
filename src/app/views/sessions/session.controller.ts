module flexportal {
  'use strict';
  interface ISessionProperties extends ng.IScope {
    id: string
    id1: string
    id2: number
    name: string
    duplicates: number []
  }

  interface IRouteParamsService extends angular.ui.IStateParamsService {
    id: string
    id1: string
    id2: number
  }

  export class SessionController {
    /* @ngInject */
    constructor($scope: ISessionProperties, $routeParams: IRouteParamsService, private $location: ng.ILocationService) {
      var id = 'not passed';
      $scope.id = $routeParams.id;
      $scope.id1 = $routeParams.id1;
      $scope.id2 = $routeParams.id2;
      $scope.name = 'Seemant';
      $scope.duplicates = [];
      
      for(var i = 0; i < 10; i++) $scope.duplicates.push(i);
    }
  }
}
