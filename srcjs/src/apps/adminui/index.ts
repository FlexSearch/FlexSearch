/// <reference path="../../../typings/index.d.ts" />
/// <reference path="../../common/partials/main.controller.ts" />
/// <reference path="../../common/references/references.d.ts" />
/// <reference path="indices.ts" />
/// <reference path="newIndex.ts" />
/// <reference path="indexDetails.ts" />
/// <reference path="editField.ts" />
/// <reference path="editQuery.ts" />

module flexportal {
  'use strict';

  var basePath = apiHelpers.getBasePath();

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial',
    'ui.router', 'chart.js', 'jsonFormatter', 'ui.grid', 'ui.grid.selection',
    'ui.grid.pagination', 'ui.grid.exporter', 'ui.ace', 'ui.grid.resizeColumns',
    'ngMdIcons'])
    // Controllers
    .controller('MainCtrl', ["$scope", "$mdUtil", "$mdSidenav", "$mdBottomSheet", MainCtrl])
    .controller('ErrorController', ErrorController)
    .controller('IndicesController', IndicesController)
    .controller('NewIndexController', NewIndexController)
    .controller('IndexDetailsController', IndexDetailsController)
    .controller('EditFieldController', EditFieldController)
    .controller('EditQueryController', EditQueryController)
    // Services
    .service('indicesApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.IndicesApi($http, null, basePath, $mdBottomSheet, $q, errorHandler); }])
    .service('documentsApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.DocumentsApi($http, null, basePath, $mdBottomSheet, $q, errorHandler); }])
    .service('analyzerApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.AnalyzerApi($http, null, basePath, $mdBottomSheet, $q, errorHandler); }])

    // Theming
    .config(function($mdThemingProvider: ng.material.IThemingProvider) {
      $mdThemingProvider.theme('default')
        .primaryPalette('blue')
        .accentPalette('red');
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

        .state('indexDetails', {
          url: "^/admin-indices/:indexName",
          parent: "main",
          templateUrl: "indexDetails.html",
          controller: "IndexDetailsController"
        })

        .state('indexDetails.edit-field', {
          url: "/edit-field?fieldName",
          templateUrl: "editField.html",
          controller: "EditFieldController"
        })

        .state('indexDetails.edit-query', {
          url: "/edit-query?queryName",
          templateUrl: "editQuery.html",
          controller: "EditQueryController"
        })
    });
}
