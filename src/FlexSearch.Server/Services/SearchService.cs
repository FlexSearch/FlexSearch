//// --------------------------------------------------------------------------------------------------------------------
//// <copyright file="SearchService.cs" company="">
////   
//// </copyright>
//// --------------------------------------------------------------------------------------------------------------------
//namespace FlexSearch.Server.Services
//{
//    using FlexSearch.Api.Search;
//    using FlexSearch.Api.Types;
//    using FlexSearch.Core;

//    using ServiceStack.ServiceInterface;

//    public class SearchService : Service
//    {
//        #region Public Properties

//        public Interface.IIndexService IndexingService { get; set; }

//        #endregion

//        #region Public Methods and Operators

//        public SearchProfileQueryResponse Any(SearchProfileQuery request)
//        {
//            var result = this.IndexingService.PerformQuery(request.IndexName, IndexQuery.NewSearchProfileQuery(request));
//            return new SearchProfileQueryResponse
//                {
//                   Documents = result.Documents, RecordsReturned = result.RecordsReturned 
//                };
//        }

//        public SearchQueryResponse Any(SearchQuery request)
//        {
//            var result = this.IndexingService.PerformQuery(request.IndexName, IndexQuery.NewSearchQuery(request));
//            return new SearchQueryResponse { Documents = result.Documents, RecordsReturned = result.RecordsReturned };
//        }

//        #endregion
//    }
//}