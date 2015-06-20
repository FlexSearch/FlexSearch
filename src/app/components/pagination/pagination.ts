// /// <reference path="../../references/references.d.ts" />

// module flexportal {
//   'use strict';
  
//     interface IPaginationScope extends ng.IScope {
//         Pages: any []
//         PageCount: number
//         PageSize: number                    // implement
//         CurrentPage: number
//         getPage(pageNumber: number): void   // implement
//         getPageCount(): ng.IPromise<number> // implement
//     }

//     export class PaginationController {
        
//         // The parent controller will need to implement the getPage function
//         constructor($scope: IPaginationScope) {
//             if ($scope.CurrentPage == undefined) $scope.CurrentPage = 1;
            
//             $scope.getPageCount()
//             .then(function(pageCount) {
//                $scope.PageCount = pageCount; 
//             });
//         }
//         // The list of page controls that will be displayed on the menu
//         pages: function(){
//             var actPage = parseInt(this.get('activePage')),
//                 pageCnt = this.get('pageCount'),
//                 pages = [];
    
//             switch(actPage){
//                 case(1): pages = [
//                     { body: '<i class="angle left icon"></i>', idx: actPage },
//                     { body: actPage, idx: actPage } ,
//                     { body: '<i class="angle right icon"></i>', idx: actPage + 1} ];
//                     break;
//                 case(pageCnt): pages = [
//                     { body: '<i class="angle left icon"></i>', idx: actPage - 1 },
//                     { body: actPage, idx: actPage } ,
//                     { body: '<i class="angle right icon"></i>', idx: actPage} ];
//                     break;
//                 default: pages = [
//                     { body: '<i class="angle left icon"></i>', idx: actPage - 1 },
//                     { body: actPage, idx: actPage } ,
//                     { body: '<i class="angle right icon"></i>', idx: actPage + 1} ];
//                     break;
//             }
    
//             return pages;
//         }.property('activePage', 'pageCount'),
//         pageCount: function(){
//             return Math.ceil(this.get('itemsCount') / this.get('pageSize'));
//         }.property('itemsCount', 'pageSize'),
//         tagName: ''
//     }
// }