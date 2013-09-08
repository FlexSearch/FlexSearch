namespace FlexSearch.Tests.CSharp.Search
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using NUnit.Framework;

    public class PhraseQueryTests : SearchTestsBase
    {
        public class TestFactory
        {
            #region Public Properties

            public static IEnumerable PhraseMatchCases
            {
                get
                {
                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                {
                                    new SearchCondition(
                                        "occupation",
                                        "phrase_match",
                                        new StringList { "Licensed clinical social worker" })
                                }),
                            TestDataFactory.GetContactTestData()
                                .Count(
                                    x =>
                                        string.Equals(
                                            x.Occupation,
                                            "Licensed clinical social worker",
                                            StringComparison.OrdinalIgnoreCase)),
                            100).SetName("Phrase match where occupation is 'Clinical social worker'");
                }
            }

            #endregion

            #region Public Methods and Operators

            [Test]
            [TestCaseSource(typeof(TestFactory), "PhraseMatchCases")]
            public void Phrase_match_related(SearchFilter rootFilter, int expectedCount, int resultsToFetch)
            {
                var searchQuery = new SearchQuery(new StringList { "occupation" }, "contact", resultsToFetch, rootFilter);
                SearchResults results = indexService.PerformQuery("contact", IndexQuery.NewSearchQuery(searchQuery));
                Assert.AreEqual(expectedCount, results.RecordsReturned);
            }

            #endregion
        }
    }
}