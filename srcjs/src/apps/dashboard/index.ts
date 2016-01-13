/// <reference path="../../../typings/tsd.d.ts" />

/// <reference path="../../common/partials/main.controller.ts" />
/// <reference path="../../common/client/api.d.ts" />
/// <reference path="../../common/references/references.d.ts" />


/// <reference path="cluster.ts" />
/// <reference path="indexDetails.ts" />

module flexportal {
  'use strict';

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial',
    'ui.router', 'chart.js', 'jsonFormatter', 'swaggerUi', 'ui.grid', 'ui.grid.selection', 
    'ui.grid.pagination', 'ui.grid.exporter', 'ui.ace', 'ui.grid.resizeColumns', 
    'ngMdIcons'])
  // Controllers
    .controller('MainCtrl', ["$scope", "$mdUtil", "$mdSidenav", "$mdBottomSheet", MainCtrl])
    .controller('ClusterController', ClusterController)
    .controller('ErrorController', ErrorController)
    .controller('IndexDetailsController', ["$scope", "$stateParams", IndexDetailsController])
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
      $urlRouterProvider.otherwise("/dashboard");

      $stateProvider
          
      // Session URLs
        .state('main', {
          url: "/main",
          templateUrl: "partials/main.html",
          controller: 'MainCtrl'
        })
        
      .state('dashboard', {
          url: "^/dashboard",
          parent: 'main',
          controller: 'ClusterController',
          templateUrl: "cluster.html"
        })
    });
}
