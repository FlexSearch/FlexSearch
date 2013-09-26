namespace FlexSearch.Specs.IntegrationTests.Search
{
    using System;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class SortingSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [IntegrationAutoFixture]
        public void SimpleSortingCase(Interface.IIndexService indexService, Index index)
        {
            string testData = @"
id,topic,surname,cvv2
1,a,jhonson,1
2,c,hewitt,1
3,b,Garner,1
4,e,Garner,1
5,d,jhonson,1
";
            SearchResults results = null;

            "Given an indexservice".Given(() => { });

            "when a new index is created with 5 records, searching for 'cvv2 = 1' with sort on givenname".When(
                () =>
                {
                    index.IndexName = Guid.NewGuid().ToString("N");
                    indexService.AddIndex(index);
                    MockHelpers.AddTestDataToIndex(indexService, index, testData);

                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetTermMatchCondition("cvv2", "1") }));
                    searchQuery.OrderBy = "topic";
                    searchQuery.Columns.Add("topic");
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                });

            "it should return 5 records".Observation(() => results.RecordsReturned.Should().Be(5));
            "1st record should be a".Observation(() => results.Documents[0].Fields["topic"].Should().Be("a"));
            "2nd record should be b".Observation(() => results.Documents[1].Fields["topic"].Should().Be("b"));
            "3rd record should be c".Observation(() => results.Documents[2].Fields["topic"].Should().Be("c"));
            "4th record should be d".Observation(() => results.Documents[3].Fields["topic"].Should().Be("d"));
            "5th record should be e".Observation(() => results.Documents[4].Fields["topic"].Should().Be("e"));
            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }

        #endregion
    }
}