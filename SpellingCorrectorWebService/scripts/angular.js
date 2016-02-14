(function () {
    'use strict';

    angular
        .module('app')
        .controller('angular', angular);

    angular.$inject = ['$scope']; 

    function angular($scope) {
        $scope.title = 'angular';

        activate();

        function activate() { }
    }
})();
