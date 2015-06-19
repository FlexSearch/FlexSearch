/// <reference path="../../typings/tsd.d.ts" />

/// <reference path="../app/partials/main.controller.ts" />
/// <reference path="../app/views/sessions/session.controller.ts" />
/// <reference path="../app/views/sessions/sessions.controller.ts" />
/// <reference path="../app/views/sessions/comparison.ts" />

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
  
  // FlexSearch Client functions
  export function getRecordById(indexName, id, $http) {
    var url = FlexSearchUrl + "/indices/" + indexName + "/documents/" + id + "?c=*";
    return $http.get(url);
  }
  
  // Error Handling
  export function errorHandler(e) {
    console.log(e); // TODO
  }

  angular.module('flexportal', ['ngAnimate', 'ngTouch', 'ngSanitize', 'restangular', 'ngMaterial', 'ui.router'])
    .controller('MainCtrl', ["$scope", "$mdUtil", "$mdSidenav", MainCtrl])
    .controller('SessionController', ["$scope", "$stateParams", "$http", "$state", SessionController])
    .controller('SessionsController', SessionsController)
    .controller('ComparisonController', ["$scope", "$stateParams", ComparisonController])
    .config(function($mdThemingProvider: ng.material.MDThemingProvider) {
      $mdThemingProvider.theme('default')
        .primaryPalette('blue')
        .accentPalette('teal');
    })
    .config(function($stateProvider: angular.ui.IStateProvider, $urlRouterProvider: angular.ui.IUrlRouterProvider) {
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
          url: "^/session/:sessionId",
          parent: 'main',
          views: {
            "": {
              templateUrl: "app/views/sessions/session.html",
              controller: 'SessionController'
            },
            "topMenuToolbar": {
              templateUrl: "app/views/sessions/session.toolbar.html",
              controller: 'SessionController'
            }
          }
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
        });

    });
}
