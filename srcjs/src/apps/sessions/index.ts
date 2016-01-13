/// <reference path="../../../typings/tsd.d.ts" />

/// <reference path="../../common/partials/main.controller.ts" />
/// <reference path="../../common/client/api.d.ts" />
/// <reference path="../../common/references/references.d.ts" />


/// <reference path="session.ts" />
/// <reference path="comparison.ts" />
/// <reference path="sessions.ts" />
/// <reference path="sessionsNew.ts" />

module flexportal {
  'use strict';

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial',
    'ui.router', 'chart.js', 'jsonFormatter', 'swaggerUi', 'ui.grid', 'ui.grid.selection', 
    'ui.grid.pagination', 'ui.grid.exporter', 'ui.ace', 'ui.grid.resizeColumns', 
    'ngMdIcons'])
  // Controllers
    .controller('MainCtrl', ["$scope", "$mdUtil", "$mdSidenav", "$mdBottomSheet", MainCtrl])
    .controller('SessionController', ["$scope", "$stateParams", "$http", "$state", "datePrinter", "flexClient", SessionController])
    .controller('SessionsController', ["$scope", "$state", "$http", "datePrinter", "flexClient", "$mdSidenav", "$mdUtil", SessionsController])
    .controller('ComparisonController', ["$scope", "$stateParams", "$mdToast", "flexClient", ComparisonController])
    .controller('SessionsNewController', ["$scope", "flexClient", "$mdToast", "$state", SessionsNewController])
  // Services
    .service('flexClient', ["$http", "$mdBottomSheet", "$q", "$location", function($http, $mdBottomSheet, $q, $location) { return new FlexClient($http, $mdBottomSheet, $q, $location); }])
    
  // Theming
    .config(function($mdThemingProvider: ng.material.IThemingProvider) {
      $mdThemingProvider.theme('default')
        .primaryPalette('blue')
        .accentPalette('grey');
    })
    
  // Route configuration
    .config(function($stateProvider: angular.ui.IStateProvider, $urlRouterProvider: angular.ui.IUrlRouterProvider) {
      $urlRouterProvider.otherwise("/sessions");

      $stateProvider
          
      // Session URLs
        .state('main', {
          url: "/main",
          templateUrl: "partials/main.html",
          controller: 'MainCtrl'
        })
        
      state('sessions', {
          url: "^/sessions",
          templateUrl: "sessions.html",
          controller: 'SessionsController',
          parent: 'main'
        })
        .state('sessionsNew', {
          url: "/new",
          templateUrl: "sessionsNew.html",
          controller: 'SessionsNewController',
          parent: 'sessions'
        })
        .state('session', {
          url: "^/session/:sessionId",
          parent: 'main',
          controller: 'SessionController',
          templateUrl: "session.html"
        })
    });
}
