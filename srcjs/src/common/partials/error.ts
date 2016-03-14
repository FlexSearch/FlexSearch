/// <reference path="../references/references.d.ts" />

module flexportal {
  'use strict';

  export function errorHandler($q, $mdBottomSheet, e) {
    var message = "An error occured";
    var errorCode, errorMessage, errorProperties;
    
    if (typeof e == "string")
      message = e;
    else if (e.data != undefined && e.data.error != undefined && e.data.error.operationCode != undefined) {
      errorCode = e.data.error.operationCode;
      errorMessage = e.data.error.message;
      errorProperties = e.data.error.properties;
    }
      
    $mdBottomSheet.show({
        templateUrl: 'partials/error.html',
        controller: 'ErrorController',
        locals: {
          message: message,
          errorCode: errorCode,
          errorMessage: errorMessage,
          errorProperties: errorProperties }
      });
      
    return $q.reject(e);
  }

  interface IErrorScope {
    errorMessage: string
    message: string
    errorCode: string
    errorProperties: string
  }

  export class ErrorController {
    /* @ngInject */
    constructor ($scope: IErrorScope, message, errorCode, errorMessage, errorProperties) {
      $scope.errorCode = errorCode;
      $scope.errorMessage = errorMessage;
      $scope.message = message;
      $scope.errorProperties = errorProperties;
    }
  }
}
