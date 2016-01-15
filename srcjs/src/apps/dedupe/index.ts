/// <reference path="../../../typings/tsd.d.ts" />
/// <reference path="../../common/partials/main.controller.ts" />
/// <reference path="../../common/references/references.d.ts" />


/// <reference path="session.controller.ts" />
/// <reference path="sessions.controller.ts" />
/// <reference path="comparison.ts" />
/// <reference path="sessionsNew.ts" />

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
        'ui.router', 'chart.js', 'jsonFormatter', 'swaggerUi', 'ui.grid', 'ui.grid.selection',
        'ui.grid.pagination', 'ui.grid.exporter', 'ui.ace', 'ui.grid.resizeColumns',
        'ngMdIcons'])
        // Controllers
        .controller('MainCtrl', ["$scope", "$mdUtil", "$mdSidenav", "$mdBottomSheet", MainCtrl])
        .controller('SessionController', ["$scope", "$stateParams", "$http", "$state", "datePrinter", "commonApi", SessionController])
        .controller('SessionsController', ["$scope", "$state", "$http", "datePrinter", "commonApi", "$mdSidenav", "$mdUtil", SessionsController])
        .controller('ComparisonController', ["$scope", "$stateParams", "$mdToast", "indicesApi", "commonApi", ComparisonController])
        .controller('SessionsNewController', ["$scope", "commonApi", "indicesApi", "$mdToast", "$state", SessionsNewController])
        // Services
        .service('indicesApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.IndicesApi($http, null, null, $mdBottomSheet, $q, errorHandler); }])
        .service('commonApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.CommonApi($http, null, null, $mdBottomSheet, $q, errorHandler); }])
        .service('datePrinter', function() {
            this.toDateStr = function(dateStr: any) {
                var date = new Date(dateStr);
                return date.toLocaleDateString() + ", " + date.toLocaleTimeString();
            };
        })
    
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

                .state('sessions', {
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
