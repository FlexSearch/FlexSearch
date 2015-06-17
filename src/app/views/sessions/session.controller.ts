module flexportal {
  'use strict';
  interface ISessionProperties extends ng.IScope {
    id: string
    id1: string
    id2: number
    name: string
    duplicates: any []
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
      $scope.duplicates = [
        {
            Source: {Name: "Vladimir Negacevschi", Status: "Processed", Quality: 2 },
            Targets: [
                {Name: "Vladimir", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"},
                {Name: "Vlad", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"},
                {Name: "Vladi", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"} ]
        },
        {
            Source: {Name: "Seemant Rajvanshi", Status: "Processed", Quality: 3},
            Targets: [
                {Name: "See", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"},
                {Name: "Raj", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"},
                {Name: "Ant", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"} ]
        },
        {
            Source: {Name: "Scott Hanselman", Status: "Processed", Quality: 4},
            Targets: [
                {Name: "Hansel", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"},
                {Name: "Man", Score: 65, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"},
                {Name: "Scooby", Score: 83, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"} ]
        },
        {
            Source: {Name: "Elliot Fu", Status: "Processed", Quality: 5},
            Targets: [
                {Name: "Fu", Score: 88, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"},
                {Name: "Elliot", Score: 100, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"}]
        },
        {
            Source: {Name: "Helen Troy", Status: "Processed", Quality: 1},
            Targets: [
                {Name: "Helen", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"}]
        },
        {
            Source: {Name: "Iron Man", Status: "Processed", Quality: 1},
            Targets: [
                {Name: "Steel", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"},
                {Name: "Boy", Score: 95, trId: "020d2294-c4ee-e011-a3a1-00237dec467a", srId: "47673f68-b677-df11-949d-00237dec9650"}]
                
        }];
      var dup = $scope.duplicates;
      
      $scope.duplicates = dup;//dup.concat(dup,dup,dup);
      
    }
  }
}
