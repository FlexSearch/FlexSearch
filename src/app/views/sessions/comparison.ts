module flexportal {
  
  export class ComparisonController {
    /* @ngInject */
    constructor($scope: any) {
      $scope.$on('$viewContentLoaded', function(event){
        (<any>$('.ui.checkbox')).checkbox();
      });
    }
  }
}
