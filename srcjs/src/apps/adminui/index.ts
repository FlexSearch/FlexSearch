/// <reference path="../../../typings/index.d.ts" />
/// <reference path="../../common/partials/main.controller.ts" />
/// <reference path="../../common/references/references.d.ts" />
/// <reference path="indices.ts" />
/// <reference path="newIndex.ts" />

module flexportal {
  'use strict';

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial',
    'ui.router', 'chart.js', 'jsonFormatter', 'ui.grid', 'ui.grid.selection',
    'ui.grid.pagination', 'ui.grid.exporter', 'ui.ace', 'ui.grid.resizeColumns',
    'ngMdIcons'])
    // Controllers
    .controller('MainCtrl', ["$scope", "$mdUtil", "$mdSidenav", "$mdBottomSheet", MainCtrl])
    .controller('ErrorController', ErrorController)
    .controller('IndicesController', IndicesController)
    .controller('NewIndexController', NewIndexController)
    // Services
    .service('indicesApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.IndicesApi($http, null, null, $mdBottomSheet, $q, errorHandler); }])
    .service('documentsApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.DocumentsApi($http, null, null, $mdBottomSheet, $q, errorHandler); }])

    // Theming
    .config(function($mdThemingProvider: ng.material.IThemingProvider) {
      $mdThemingProvider.theme('default')
        .primaryPalette('blue')
        .accentPalette('grey');
    })

    // Route configuration
    .config(function($stateProvider: angular.ui.IStateProvider, $urlRouterProvider: angular.ui.IUrlRouterProvider) {
      $urlRouterProvider.otherwise("/admin-indices");

      $stateProvider

        // Main URLs
        .state('main', {
          url: "/main",
          templateUrl: "partials/main.html",
          controller: 'MainCtrl'
        })

        // Indices
        .state('admin-indices', {
          url: "^/admin-indices",
          parent: "main",
          templateUrl: "indices.html",
          controller: "IndicesController"
        })

        .state('admin-indices.new', {
          url: "/new",
          templateUrl: "newIndex.html",
          controller: "NewIndexController"
        })
    });
}
