/// <reference path="..\..\references\references.d.ts" />

module flexportal {
    'use strict';

    export interface ISessionToolbarScope extends ng.IScope {
        targetToProcess: string
        
    }

    export class SessionToolbarController {
        /* @ngInject */
        constructor($scope: ISessionToolbarScope, $http: ng.IHttpService) {
            $scope.targetToProcess = null;
            $scope.$on('selectedTargetChanged', function(event, newValue) {
               $scope.targetToProcess = newValue; 
            });
        }
    }
}
