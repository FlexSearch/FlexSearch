/// <reference path="../../common/references/references.d.ts" />
/// <reference path="./indexDetails.ts"/>

module flexportal {
  'use strict';

  import FieldTypeEnum = API.Client.Field.FieldTypeEnum
  import FieldSimilarityEnum = API.Client.Field.SimilarityEnum

  interface IEditFieldScope extends ng.IScope, IMainScope, IIndexDetailsScope {
    fieldName: string
    fieldType: FieldTypeEnum
    allowSort: boolean
    indexAnalyzer: string
    searchAnalyzer: string
    similarity: FieldSimilarityEnum
    analyzers: string[]
    similarities: string[]
    isNewField: boolean
    submit()
  }

  export class EditFieldController {
    /* @ngInject */
    constructor($scope: IEditFieldScope, indicesApi: API.Client.IndicesApi, documentsApi: API.Client.DocumentsApi, analyzerApi: API.Client.AnalyzerApi,
      $q: any, $state: any, $mdToast: any, $timeout: any, $stateParams: any) {

      // Are we editing an existing field or creating a new one?
      $scope.isNewField = !$stateParams.fieldName;
      if ($scope.isNewField) {
        // Set default values
        $scope.allowSort = false;
        $scope.indexAnalyzer = "standard";
        $scope.searchAnalyzer = "standard";
        $scope.similarity = FieldSimilarityEnum.TFIDF;
      }
      else {
        let field = $scope.index.fields.filter(f => f.fieldName == $stateParams.fieldName)[0];
        $scope.fieldName = field.fieldName;
        $scope.fieldType = field.fieldType;
        $scope.allowSort = field.allowSort;
        $scope.indexAnalyzer = field.indexAnalyzer;
        $scope.searchAnalyzer = field.searchAnalyzer;
        $scope.similarity = field.similarity;
      }
      // Get all the analyzers available on the server
      analyzerApi.getAllAnalyzersHandled()
        .then(r => $scope.analyzers = r.data.map(a => a.analyzerName));

      // List the available Field Similarity options
      $scope.similarities = Object.keys(FieldSimilarityEnum).map(k => FieldSimilarityEnum[k]).filter(v => typeof v === "string");

      $scope.submit = function() {
        let newField = {
          fieldName: $scope.fieldName,
          fieldType: $scope.fieldType,
          allowSort: $scope.allowSort,
          indexAnalyzer: $scope.indexAnalyzer,
          searchAnalyzer: $scope.searchAnalyzer,
          similarity: $scope.similarity
        };

        if ($scope.isNewField) {
          $scope.index.fields.push(newField);
        }
        else {
          let position = $scope.index.fields
            .map((f, i) => ({ position: i, value: f }))
            .filter(x => x.value.fieldName == $scope.fieldName)[0]
            .position;

          $scope.index.fields[position] = newField;
        }

        $scope.working = true;
        indicesApi.updateIndexFieldsHandled({ fields: $scope.index.fields }, $scope.indexName)
          .then(r => {
            $mdToast.show(
              $mdToast.simple()
                .content("Field " + $scope.fieldName + " processed successfully.")
                .position("top right")
                .hideDelay(3000));
            $scope.working = false;
            $state.go('indexDetails', {}, { reload: true });
          });
      };
    }
  }
}
