/// <reference path="../../typings/tsd.d.ts" />

/// <reference path="../app/partials/main.controller.ts" />
/// <reference path="../app/views/sessions/session.controller.ts" />
/// <reference path="../app/views/sessions/sessions.controller.ts" />

module flexportal {
  'use strict';

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial', 'ui.router'])
    .controller('MainCtrl', ["$scope", "$mdUtil", "$mdSidenav", MainCtrl])
    .controller('SessionController', ["$scope", "$stateParams", SessionController])
    .controller('SessionsController', SessionsController)
    .config(function($mdThemingProvider: ng.material.MDThemingProvider) {
      $mdThemingProvider.theme('default')
        .primaryPalette('blue')
        .accentPalette('teal');
    })
    .config(function($stateProvider: angular.ui.IStateProvider, $urlRouterProvider : angular.ui.IUrlRouterProvider){
      $urlRouterProvider.otherwise("/main");

      $stateProvider
        .state('main', {
          url: "/main",
          templateUrl: "app/partials/main.html",
          controller: 'MainCtrl'
        })
        .state('todo', {
          url: "/todo",
          template: "<h1>To be implemented</h1>",
          parent: 'main'
        })
        .state('sessions', {
          url: "^/sessions",
          templateUrl: "app/views/sessions/sessions.html",
          controller: 'SessionsController',
          parent: 'main'
        })
        .state('session', {
          url: "^/session/:id",
          templateUrl: "app/views/sessions/session.html",
          controller: 'SessionController',
          parent: 'main'
        });
    });
}
