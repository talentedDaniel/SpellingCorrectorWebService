//<reference path="angular.js" />
var app = angular
        .module("spellCorrectorModule", [])
        .controller("spellCorrectorController", function ($scope, $http){
            $http.get('SpellingCorrectorWebService.asmx/GetCorrection')
            .then(function (response) {
                $scope.result = response.data;
            });

            $scope.getCallJSON = function () {
                var params = {
                    jsonObjParam: {
                        param1: $scope.getJSONParam1,
                    }
                };

                var config = {
                    params: params
                };

                $http.get("SpellingCorrectorWebService.asmx", config)
                  .success(function (data, status, headers, config) {

                      data = jsonFilter(data);

                      $scope.getCallJSONResult = logResult("GET SUCCESS", data, status, headers, config);
                  })
                  .error(function (data, status, headers, config) {
                      $scope.getCallJSONResult = logResult("GET ERROR", data, status, headers, config);
                  });
            };
        });