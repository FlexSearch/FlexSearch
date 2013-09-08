// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NumericRangeQueryTests.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace FlexSearch.Tests.CSharp.Search
{
    using System.Collections;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using NUnit.Framework;
    using System.Linq;


    [TestFixture]
    public class NumericRangeQueryTests : SearchTestsBase
    {
        #region Public Methods and Operators

        [Test]
        [TestCaseSource(typeof(TestFactory), "NumericRangeQueryCases")]
        public void Numeric_range_related(SearchFilter rootFilter, int expectedCount, int resultsToFetch)
        {
            var searchQuery = new SearchQuery(null, "contact", resultsToFetch, rootFilter);
            var results = indexService.PerformQuery("contact", IndexQuery.NewSearchQuery(searchQuery));
            Assert.AreEqual(expectedCount, results.RecordsReturned);
        }

        #endregion

        public class TestFactory
        {
            #region Public Properties

            public static IEnumerable NumericRangeQueryCases
            {
                get
                {
                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                    {
                                        new SearchCondition("cvv2", "numeric_range", new StringList { "1", "5" })
                                            {
                                                Parameters =
                                                    new KeyValuePairs
                                                        {
                                                           { "includelower", "true" }, { "includeupper", "true" } 
                                                        }
                                            }
                                    }), 
                            TestDataFactory.GetContactTestData().Count(x => x.CVV2 >= 1 && x.CVV2 <= 5), 
                            100).SetName("Numeric range query where cvv2 between 1 and 5 inclusive");

                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                    {
                                        new SearchCondition("cvv2", "numeric_range", new StringList { "1", "5" })
                                            {
                                                Parameters = new KeyValuePairs
                                                        {
                                                           { "includelower", "false" }, { "includeupper", "false" } 
                                                        }
                                            }
                                    }),
                            TestDataFactory.GetContactTestData().Count(x => x.CVV2 > 1 && x.CVV2 < 5), 
                            100).SetName("Numeric range query where cvv2 between 1 and 5 exclusive");

                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                    {
                                        new SearchCondition("cvv2", "numeric_range", new StringList { "1", "5" })
                                            {
                                                Parameters =
                                                    new KeyValuePairs
                                                        {
                                                           { "includelower", "false" }, { "includeupper", "true" } 
                                                        }
                                            }
                                    }),
                            TestDataFactory.GetContactTestData().Count(x => x.CVV2 > 1 && x.CVV2 <= 5), 
                            100).SetName("Numeric range query where cvv2 between 1 and 5 upper inclusive");

                    yield return
                        new TestCaseData(
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                    {
                                        new SearchCondition("cvv2", "numeric_range", new StringList { "1", "5" })
                                            {
                                                Parameters =
                                                    new KeyValuePairs
                                                        {
                                                           { "includelower", "true" }, { "includeupper", "false" } 
                                                        }
                                            }
                                    }),
                            TestDataFactory.GetContactTestData().Count(x => x.CVV2 >= 1 && x.CVV2 < 5), 
                            100).SetName("Numeric range query where cvv2 between 1 and 5 lower inclusive");
                }
            }

            #endregion
        }
    }
}