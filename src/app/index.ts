/// <reference path="../../typings/tsd.d.ts" />

/// <reference path="main/main.controller.ts" />
/// <reference path="../app/components/navbar/navbar.controller.ts" />
/// <reference path="../app/sessions/session.controller.ts" />
/// <reference path="../app/sessions/sessions.controller.ts" />

module flexportal {
  'use strict';

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngRoute', 'ngMaterial'])
    .controller('MainCtrl', MainCtrl)
    .controller('NavbarCtrl', NavbarCtrl)
    .controller('SessionController', SessionController)
    .controller('SessionsController', SessionsController)
    .config(function($mdThemingProvider: ng.material.MDThemingProvider) {
      $mdThemingProvider.theme('default')
        .primaryPalette('green')
        .accentPalette('teal');
    })
    .config(function($routeProvider: ng.route.IRouteProvider) {
      $routeProvider
        .when('/main', {
          templateUrl: 'app/main/main.html',
          controller: MainCtrl
        })
        .when('/session/:id', {
          templateUrl: 'app/sessions/session.html',
          controller: SessionController
        })
        .when('/sessions', {
          templateUrl: 'app/sessions/sessions.html',
          controller: SessionsController
        })
        .otherwise({
          redirectTo: '/main'
        });
    })
  ;
}
