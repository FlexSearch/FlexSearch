// angular.module('gridListDemo1', ['ngMaterial'])
// .controller('AppCtrl', function($scope) {});

var app = angular.module('Dedupe', ['ngMaterial']);

	app.controller('AppCtrl', ['$scope', '$mdSidenav', function($scope, $mdSidenav){
	  $scope.toggleSidenav = function(menuId) {
	    $mdSidenav(menuId).toggle();
	  };
	 
	}]);

