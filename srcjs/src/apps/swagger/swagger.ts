/// <reference path="../../common/references/references.d.ts" />

module flexportal {
  'use strict';

  interface ISwaggerScope extends ng.IScope, IMainScope {
    swaggerUrl : string
    swaggerErrorHandler(response,status) : void
    transformFunction(options) : void
  }

  export class SwaggerController {
    /* @ngInject */
    constructor($scope: ISwaggerScope, flexClient: FlexClient) {
      // Modify the URL so that it uses the base URL in the browser
      $scope.transformFunction = function(options) {
        options.url = options.url.replace(new RegExp("http[s]?:\/\/[^:]+:[0-9]+"), flexClient.FlexSearchUrl);
      };
      
      $scope.swaggerUrl = "swagger.json";
      $scope.swaggerErrorHandler = function(response, status) {
        $scope.showError("Response:\n" + response.toString() + "\n\nStatus:\n" + status.toString());
      };
      
    }
  }
}
