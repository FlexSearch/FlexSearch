/// <reference path="../../typings/tsd.d.ts" />

/// <reference path="../app/partials/main.controller.ts" />
/// <reference path="../app/partials/error.ts" />
/// <reference path="../app/views/sessions/session.controller.ts" />
/// <reference path="../app/views/sessions/sessions.controller.ts" />
/// <reference path="../app/views/sessions/comparison.ts" />
/// <reference path="../app/services/flexClient.ts" />
/// <reference path="../app/views/sessions/sessionsNew.ts" />
/// <reference path="../app/views/search/searchProfile.ts" />
/// <reference path="../app/views/search/searchProfileSettings.ts" />
/// <reference path="../app/views/search/searchBase.ts" />
/// <reference path="../app/views/search/search.ts" />
/// <reference path="../app/views/search/searchSettings.ts" />
/// <reference path="../app/views/searchstudio/searchstudio.ts" />
/// <reference path="../app/views/dashboard/cluster.ts" />
/// <reference path="../app/views/dashboard/indexDetails.ts" />
/// <reference path="../app/views/swagger/swagger.ts" />
/// <reference path="../app/views/analyzer/analyzerTest.ts" />
/// <reference path="../app/views/home/home.ts" />
/// <reference path="../app/views/demoindex/demoindex.ts" />

module flexportal {
  'use strict';

  // Functions that map Option Sets
  export function toSourceStatusName(value: number) {
    switch (value) {
      case 0: return { Name: "Proposed", Icon: "speaker_notes" };
      case 1: return { Name: "Reviewed", Icon: "flag" };
      case 2: return { Name: "Processed", Icon: "done" };
      default: return { Name: "Proposed", Icon: "speaker_notes" };
    }
  }

  // Helpers
  export function firstOrDefault(a: any[], fieldName, value) {
    var r = a.filter(x => x[fieldName] == value);
    if (r.length > 0) return r[0];
    return null;
  }

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial',
    'ui.router', 'chart.js', 'jsonFormatter', 'swaggerUi', 'ui.grid', 'ui.grid.selection', 'ui.grid.pagination', 'ui.grid.exporter', 'ui.ace'])
  // Controllers
    .controller('MainCtrl', ["$scope", "$mdUtil", "$mdSidenav", "$mdBottomSheet", MainCtrl])
    .controller('SessionController', ["$scope", "$stateParams", "$http", "$state", "datePrinter", "flexClient", SessionController])
    .controller('SessionsController', ["$scope", "$state", "$http", "datePrinter", "flexClient", "$mdSidenav", "$mdUtil", SessionsController])
    .controller('ComparisonController', ["$scope", "$stateParams", "$mdToast", "flexClient", ComparisonController])
    .controller('SessionsNewController', ["$scope", "flexClient", "$mdToast", "$state", SessionsNewController])
    .controller('SearchBaseController', ["$scope", "flexClient", "$mdBottomSheet", SearchBaseController])
    .controller('SearchProfileSettingsController', ["$scope", "$mdBottomSheet", SearchProfileSettingsController])
    .controller('SearchProfileController', SearchProfileController)
    .controller('SearchController', SearchController)
    .controller('SearchSettingsController', ["$scope", "$mdBottomSheet", SearchSettingsController])
    .controller('ErrorController', ErrorController)
    .controller('ClusterController', ClusterController)
    .controller('HomeController', HomeController)
    .controller('IndexDetailsController', ["$scope", "$stateParams", IndexDetailsController])
    .controller('SwaggerController', SwaggerController)
    .controller('AnalyzerTestController', ["$scope", "flexClient", AnalyzerTestController])
    .controller('SearchStudioController', ["$scope", "flexClient", SearchStudioController])
    .controller('DemoIndexController', ["$scope", "$state", "flexClient", DemoIndexController])
  // Services
    .service('datePrinter', function() {
      this.toDateStr = function(dateStr: any) {
        var date = new Date(dateStr);
        return date.toLocaleDateString() + ", " + date.toLocaleTimeString();
      };
    })
    .service('flexClient', ["$http", "$mdBottomSheet", "$q", "$location", function($http, $mdBottomSheet, $q, $location) { return new FlexClient($http, $mdBottomSheet, $q, $location); }])
    
  // Theming
    .config(function($mdThemingProvider: ng.material.IThemingProvider) {
      $mdThemingProvider.theme('default')
        .primaryPalette('blue')
        .accentPalette('grey');
    })
    
  // Route configuration
    .config(function($stateProvider: angular.ui.IStateProvider, $urlRouterProvider: angular.ui.IUrlRouterProvider) {
      $urlRouterProvider.otherwise("/home");

      $stateProvider
          
      // Session URLs
        .state('main', {
          url: "/main",
          templateUrl: "app/partials/main.html",
          controller: 'MainCtrl'
        })
        .state('sessions', {
          url: "^/sessions",
          templateUrl: "app/views/sessions/sessions.html",
          controller: 'SessionsController',
          parent: 'main'
        })
        .state('sessionsNew', {
          url: "/new",
          templateUrl: "app/views/sessions/sessionsNew.html",
          controller: 'SessionsNewController',
          parent: 'sessions'
        })
        .state('session', {
          url: "^/session/:sessionId",
          parent: 'main',
          controller: 'SessionController',
          templateUrl: "app/views/sessions/session.html"
        })
        .state('comparison', {
          url: "/:sourceId",
          parent: 'session',
          views: {
            "comparison": {
              templateUrl: "app/views/sessions/comparison.html",
              controller: ComparisonController
            }
          }
        })
      // Dashboard
        .state('dashboard', {
          url: "^/dashboard",
          parent: 'main',
          controller: 'ClusterController',
          templateUrl: "app/views/dashboard/cluster.html"
        })
      // Home controller
        .state('home', {
          url: "^/home",
          parent: 'main',
          controller: 'HomeController',
          templateUrl: "app/views/home/home.html"
        })
        .state('indexDetails', {
          url: "/:indexName",
          parent: 'dashboard',
          controller: 'IndexDetailsController',
          templateUrl: "app/views/dashboard/indexDetails.html"
        })
        
      // Swagger
        .state('swagger', {
          url: "^/swagger",
          parent: 'main',
          controller: 'SwaggerController',
          templateUrl: "app/views/swagger/swagger.html"
        })
        
      // Analysis
        .state('analyzerTest', {
          url: "^/analyzertest",
          parent: 'main',
          controller: 'AnalyzerTestController',
          templateUrl: "app/views/analyzer/analyzerTest.html"
        })
        
      // Search Studio
        .state('searchStudio', {
          url: "^/searchstudio",
          parent: 'main',
          controller: 'SearchStudioController',
          templateUrl: "app/views/searchstudio/searchstudio.html"
        })
        
      // DemoIndex
        .state('demoindex', {
          url: "^/demoindex",
          parent: 'main',
          controller: 'DemoIndexController',
          templateUrl: "app/views/demoindex/demoindex.html"
        })
    });
}
