/// <reference path="../references/references.d.ts" />

module flexportal {
  'use strict';

  export function errorHandler($q, $mdBottomSheet, e) {
    var message = "An error occured";
    var errorCode, errorMessage, errorProperties;
    
    if (typeof e == "string")
      message = e;
    else if (e.data != undefined && e.data.Error != undefined && e.data.Error.ErrorCode != undefined) {
      errorCode = e.data.Error.ErrorCode;
      errorMessage = e.data.Error.Message;
      errorProperties = e.data.Error.Properties;
    }
      
    $mdBottomSheet.show({
        templateUrl: 'app/partials/error.html',
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
