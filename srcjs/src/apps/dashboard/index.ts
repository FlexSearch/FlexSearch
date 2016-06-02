/// <reference path="../../../typings/index.d.ts" />

/// <reference path="../../common/partials/main.controller.ts" />
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
        .service('serverApi', ["$http", "$mdBottomSheet", "$q", function($http, $mdBottomSheet, $q) { return new API.Client.ServerApi($http, null, null, $mdBottomSheet, $q, errorHandler); }])
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
                .state('indexDetails', {
                    url: "/:indexName",
                    parent: 'dashboard',
                    controller: 'IndexDetailsController',
                    templateUrl: "indexDetails.html"
                })
        });
}
