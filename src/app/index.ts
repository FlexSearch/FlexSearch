/// <reference path="../../typings/tsd.d.ts" />

/// <reference path="../app/partials/main.controller.ts" />
/// <reference path="../app/components/navbar/navbar.controller.ts" />
/// <reference path="../app/partials/session.controller.ts" />
/// <reference path="../app/partials/sessions.controller.ts" />

module flexportal {
  'use strict';

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial', 'ui.router'])
    .controller('MainCtrl', MainCtrl)
    .controller('SessionController', SessionController)
    .controller('SessionsController', SessionsController)
    .config(function($mdThemingProvider: ng.material.MDThemingProvider) {
      $mdThemingProvider.theme('default')
        .primaryPalette('green')
        .accentPalette('teal');
    })
    // .config(function($routeProvider: ng.route.IRouteProvider) {
    //   $routeProvider
    //     .when('/main', {
    //       templateUrl: 'app/main/main.html',
    //       controller: MainCtrl
    //     })
    //     .when('/session/:id', {
    //       templateUrl: 'app/sessions/session.html',
    //       controller: SessionController
    //     })
    //     .when('/sessions', {
    //       templateUrl: 'app/sessions/sessions.html',
    //       controller: SessionsController
    //     })
    //     .otherwise({
    //       redirectTo: '/main'
    //     });
    // })
    .config(function($stateProvider: angular.ui.IStateProvider, $urlRouterProvider : angular.ui.IUrlRouterProvider){
      $urlRouterProvider.otherwise("/main");
      
      $stateProvider
        .state('main', {
          url: "/main",
          templateUrl: "app/partials/main.html",
          controller: 'MainCtrl'
        })
        .state('sessions', {
          url: "/sessions",
          templateUrl: "app/partials/sessions.html",
          controller: 'SessionsController'
        })
        .state('session', {
          url: "/session/:id",
          templateUrl: "app/partials/session.html",
          controller: 'SessionController'
        });
    });
}
