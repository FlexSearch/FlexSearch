// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleSearchTests.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace FlexSearch.Tests.CSharp.Search
{
    using System;
    using System.Collections;
    using System.Linq;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using NUnit.Framework;

    [TestFixture]
    public class TermMatchTests : SearchTestsBase
    {
        #region Public Methods and Operators

        [Test]
        [TestCaseSource(typeof(TestFactory), "TermMatchCases")]
        public void Term_match_related(SearchFilter rootFilter, int expectedCount, int resultsToFetch)
        {
            var searchQuery = new SearchQuery(null, "contact", resultsToFetch, rootFilter);
            SearchResults results = indexService.PerformQuery("contact", IndexQuery.NewSearchQuery(searchQuery));
            Assert.AreEqual(expectedCount, results.RecordsReturned);
        }

        #endregion

        public class TestFactory
        {
            #region Public Properties

            public static IEnumerable TermMatchCases
            {
                get
                {
                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new[] { new SearchCondition("givenname", "term_match", new StringList { "Aaron" }) }),
                            TestDataFactory.GetContactTestData()
                                .Count(x => string.Equals(x.GivenName, "Aaron", StringComparison.OrdinalIgnoreCase)),
                            100).SetName("Term match where given name has Aaron");

                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new[]
                                {
                                    new SearchCondition("givenname", "term_match", new StringList { "Aaron" }),
                                    new SearchCondition("surname", "term_match", new StringList { "Hewitt" })
                                }),
                            TestDataFactory.GetContactTestData()
                                .Count(
                                    x =>
                                        string.Equals(x.GivenName, "Aaron", StringComparison.OrdinalIgnoreCase)
                                        && string.Equals(x.Surname, "Hewitt", StringComparison.OrdinalIgnoreCase)),
                            100).SetName("Term match where given name and surname is Aaron Hewitt");

                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new[] { new SearchCondition("givenname", "term_match", new StringList { "Aaron" }) },
                                new[]
                                {
                                    new SearchFilter(
                                        FilterType.Or,
                                        new[]
                                        {
                                            new SearchCondition("surname", "term_match", new StringList { "Garner" }),
                                            new SearchCondition("surname", "term_match", new StringList { "Hewitt" })
                                        })
                                }),
                            TestDataFactory.GetContactTestData()
                                .Count(
                                    x =>
                                        string.Equals(x.GivenName, "Aaron", StringComparison.OrdinalIgnoreCase)
                                        && (string.Equals(x.Surname, "Hewitt", StringComparison.OrdinalIgnoreCase)
                                            || string.Equals(x.Surname, "Garner", StringComparison.OrdinalIgnoreCase))),
                            100).SetName(
                                "Term match where givenname = Aaron and (surname = Hewitt or surname = Garner)");

                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new[] { new SearchCondition("cvv2", "term_match", new StringList { "991" }) }),
                            6,
                            100).SetName("Term match where cvv2 = 991");

                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new[] { new SearchCondition("id", "term_match", new StringList { "1" }) }),
                            TestDataFactory.GetContactTestData().Count(x => x.Number == 1),
                            100).SetName("Term match where id = 1");

                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new[] { new SearchCondition("type", "term_match", new StringList { "contact" }) }),
                            TestDataFactory.GetContactTestData().Count(),
                            3000).SetName("Term match where type = contact");
                }
            }

            #endregion
        }
    }
}