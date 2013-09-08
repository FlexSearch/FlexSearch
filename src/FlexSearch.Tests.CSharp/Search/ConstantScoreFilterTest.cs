namespace FlexSearch.Tests.CSharp.Search
{
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using NUnit.Framework;

    [TestFixture]
    public class ConstantScoreFilterTest : SearchTestsBase
    {
        #region Public Methods and Operators

        //[Test]
        public void All_results_should_have_a_score_of_10()
        {
            var rootFilter = new SearchFilter
                             {
                                 FilterType = FilterType.Or,
                                 Conditions =
                                     new List<SearchCondition>
                                     {
                                         new SearchCondition(
                                             "givenname",
                                             "term_match",
                                             new StringList { "Jerry" })
                                     }
                             };

            var subFilter2 = new SearchFilter(
                FilterType.Or,
                new List<SearchCondition> { new SearchCondition("givenname", "term_match", new StringList { "Aaron" }) })
                             {
                                 ConstantScore
                                     =
                                     9000000
                             };

            rootFilter.SubFilters = new List<SearchFilter> { subFilter2 };

            var searchQuery = new SearchQuery(new StringList { "givenname" }, "contact", 100, rootFilter);
            SearchResults results = indexService.PerformQuery("contact", IndexQuery.NewSearchQuery(searchQuery));
            foreach (var result in results.Documents)
            {
                if (result.Fields["givenname"] == "Aaron")
                {
                    Assert.AreEqual(100, result.Score);
                }
                //else
                //{
                //    Assert.AreEqual(50, result.Score);
                //}
            }
        }

        #endregion
    }
}