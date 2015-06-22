module flexportal {
  'use strict';

  export interface IMainScope extends ng.IScope {
    toggleSideNav(navID: string): void
    setTitle(str: string): void
  }

  export class MainCtrl {
    /* @ngInject */
    constructor ($scope: IMainScope, $mdUtil: any, $mdSidenav: any) {
      $scope.toggleSideNav = function(navID) {
        $mdUtil.debounce(function(){
            $mdSidenav(navID).toggle();
        }, 300)();
      };
      
      $scope.setTitle = function(str) {
        $('.site-content-toolbar .title').text(str);
      };
    }
  }

}
