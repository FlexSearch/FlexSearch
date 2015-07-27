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
/// <reference path="../app/views/dashboard/cluster.ts" />
/// <reference path="../app/views/dashboard/indexDetails.ts" />
/// <reference path="../app/views/swagger/swagger.ts" />

module flexportal {
  'use strict';

  // Constants
  export var FlexSearchUrl = "http://localhost:9800"
  export var DuplicatesUrl = FlexSearchUrl + "/indices/duplicates"

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
  export function firstOrDefault(a: any [], fieldName, value) {
    var r = a.filter(x => x[fieldName] == value);
    if (r.length > 0) return r[0];
    return null;
  }

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial', 
    'ui.router', 'chart.js', 'jsonFormatter', 'swaggerUi'])
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
    .controller('IndexDetailsController', ["$scope", "$stateParams", IndexDetailsController])
    .controller('SwaggerController', SwaggerController)
    
    // Services
    .service('datePrinter', function() {
      this.toDateStr = function(dateStr: any) {
        var date = new Date(dateStr);
        return date.toLocaleDateString() + ", " + date.toLocaleTimeString();
      }; 
    })
    .service('flexClient', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new FlexClient($http, $mdBottomSheet, $q);} ])
    
    // Theming
    .config(function($mdThemingProvider: ng.material.IThemingProvider) {
      $mdThemingProvider.definePalette("docs-blue", $mdThemingProvider.extendPalette("blue", {
          50: "#DCEFFF",
          100: "#AAD1F9",
          200: "#7BB8F5",
          300: "#4C9EF1",
          400: "#1C85ED",
          500: "#106CC8",
          600: "#0159A2",
          700: "#025EE9",
          800: "#014AB6",
          900: "#013583",
          contrastDefaultColor: "light",
          contrastDarkColors: "50 100 200 A100",
          contrastStrongLightColors: "300 400 A200 A400"
      }));
      $mdThemingProvider.definePalette("docs-red", 
        $mdThemingProvider.extendPalette("red", { A100: "#DE3641" }
      )); 
      $mdThemingProvider.theme("docs-dark", "default")
        .primaryPalette("light-blue").dark();
      $mdThemingProvider.theme("default")
        .primaryPalette("docs-blue").accentPalette("docs-red");
    })
    
    // Route configuration
    .config(function($stateProvider: angular.ui.IStateProvider, $urlRouterProvider: angular.ui.IUrlRouterProvider) {
      $urlRouterProvider.otherwise("/dashboard");

      $stateProvider
      
        // Session URLs
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
        
        // Search Profile URLs
        .state('searchBase', {
          abstract: true,
          url: "^/searchBase",
          parent: 'main',
          controller: 'SearchBaseController',
          templateUrl: "app/views/search/searchBase.html"
        })
        .state('searchProfile', {
          url: "^/searchProfile",
          parent: 'searchBase',
          controller: 'SearchProfileController',
          templateUrl: "app/views/search/searchProfile.html"
        })
        .state('search', {
          url: "^/search",
          parent: 'searchBase',
          controller: 'SearchController',
          templateUrl: "app/views/search/search.html"
        })

        // Dashboard
        .state('dashboard', {
          url: "^/dashboard",
          parent: 'main',
          controller: 'ClusterController',
          templateUrl: "app/views/dashboard/cluster.html"
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
    });
}
