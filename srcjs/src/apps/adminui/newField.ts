/// <reference path="../../common/references/references.d.ts" />
/// <reference path="./indexDetails.ts"/>

module flexportal {
  'use strict';

  import FieldTypeEnum = API.Client.Field.FieldTypeEnum

  interface INewFieldScope extends ng.IScope, IMainScope, IIndexDetailsScope {
    fieldName: string
    fieldType: FieldTypeEnum
    create(): void
  }

  export class NewFieldController {
    /* @ngInject */
    constructor($scope: INewFieldScope, indicesApi: API.Client.IndicesApi, documentsApi: API.Client.DocumentsApi,
      $q: any, $state: any, $mdToast: any, $timeout: any) {

      $scope.create = function() {
        let newField = {
          fieldName: $scope.fieldName,
          fieldType: $scope.fieldType
        };
        $scope.index.fields.push(newField);

        $scope.working = true;
        indicesApi.updateIndexFieldsHandled({fields: $scope.index.fields}, $scope.indexName)
        .then(r => {
          $mdToast.show(
            $mdToast.simple()
              .content("Field " + $scope.fieldName + " added successfully.")
              .position("top right")
              .hideDelay(3000));
          $scope.working = false;
          $state.go('indexDetails', {}, { reload: true });
        })
      }
    }
  }
}
