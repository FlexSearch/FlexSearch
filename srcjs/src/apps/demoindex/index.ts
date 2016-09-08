/// <reference path="../../../typings/index.d.ts" />
/// <reference path="../../common/partials/main.controller.ts" />
/// <reference path="../../common/references/references.d.ts" />


/// <reference path="demoindex.ts" />

module flexportal {
  'use strict';

  var basePath = apiHelpers.getBasePath();

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial',
    'ui.router', 'chart.js', 'jsonFormatter', 'swaggerUi', 'ui.grid', 'ui.grid.selection',
    'ui.grid.pagination', 'ui.grid.exporter', 'ui.ace', 'ui.grid.resizeColumns',
    'ngMdIcons'])
  // Controllers
    .controller('MainCtrl', ["$scope", "$mdUtil", "$mdSidenav", "$mdBottomSheet", MainCtrl])
    .controller('ErrorController', ErrorController)
    .controller('DemoIndexController', DemoIndexController)
  // Services
    .service('serverApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.ServerApi($http, null, basePath, $mdBottomSheet, $q, errorHandler); }])
    .service('indicesApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.IndicesApi($http, null, basePath, $mdBottomSheet, $q, errorHandler); }])

  // Theming
    .config(function($mdThemingProvider: ng.material.IThemingProvider) {
      $mdThemingProvider.theme('default')
        .primaryPalette('blue')
        .accentPalette('grey');
    })

  // Route configuration
    .config(function($stateProvider: angular.ui.IStateProvider, $urlRouterProvider: angular.ui.IUrlRouterProvider) {
      $urlRouterProvider.otherwise("/demoindex");

      $stateProvider

      // Session URLs
        .state('main', {
          url: "/main",
          templateUrl: "partials/main.html",
          controller: 'MainCtrl'
        })

      .state('demoindex', {
          url: "^/demoindex",
          parent: 'main',
          controller: 'DemoIndexController',
          templateUrl: "demoindex.html"
        })
    });
}
