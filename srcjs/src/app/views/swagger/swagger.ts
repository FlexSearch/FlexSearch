/// <reference path="../../references/references.d.ts" />

module flexportal {
  'use strict';

  interface ISwaggerScope extends ng.IScope, IMainScope {
    swaggerUrl : string
    swaggerErrorHandler(response,status) : void
  }

  export class SwaggerController {
    /* @ngInject */
    constructor($scope: ISwaggerScope) {
      $scope.swaggerUrl = "swagger_v2.json";
      $scope.swaggerErrorHandler = function(response, status) {
        $scope.showError("Response:\n" + response.toString() + "\n\nStatus:\n" + status.toString());
      }
    }
  }
}
