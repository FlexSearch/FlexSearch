namespace FlexSearch.Specs.IntegrationTests.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class ColumnsSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [IntegrationAutoFixture]
        public void ColumnReturnCases(Interface.IIndexService indexService, Index index)
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
            var searchQuery = new SearchQuery(
                index.IndexName,
                new SearchFilter(
                    FilterType.And,
                    new List<SearchCondition> { SearchCondition.GetTermMatchCondition("cvv2", "1") }));

            "Given an indexservice".Given(() => { });

            "when a new index is created with 5 records".When(
                () =>
                {
                    index.IndexName = Guid.NewGuid().ToString("N");
                    indexService.AddIndex(index);
                    MockHelpers.AddTestDataToIndex(indexService, index, testData);
                });

            "searching with no columns specified will return no additional columns".Observation(
                () =>
                {
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                    results.Documents[0].Fields.Count().Should().Be(0);
                });

            "searching with columns specified with '*' will return all columns".Observation(
                () =>
                {
                    searchQuery.Columns = new StringList { "*" };
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                    results.Documents[0].Fields.Count().Should().BeGreaterOrEqualTo(3);
                });

            "the returned columns should contain column 'topic'".Observation(
                () => results.Documents[0].Fields.ContainsKey("topic").Should().Be(true));

            "the returned columns should contain column 'surname'".Observation(
                () => results.Documents[0].Fields.ContainsKey("surname").Should().Be(true));

            "the returned columns should contain column 'cvv2'".Observation(
                () => results.Documents[0].Fields.ContainsKey("cvv2").Should().Be(true));

            "searching with columns specified as 'topic' will return just one column".Observation(
                () =>
                {
                    searchQuery.Columns = new StringList { "topic" };
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                    results.Documents[0].Fields.Count().Should().Be(1);
                });

            "the returned column should be 'topic'".Observation(
                () => results.Documents[0].Fields.ContainsKey("topic").Should().Be(true));

            "searching with columns specified as 'topic' & 'surname' will return just two columns".Observation(
                () =>
                {
                    searchQuery.Columns = new StringList { "topic", "surname" };
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                    results.Documents[0].Fields.Count().Should().Be(2);
                });

            "the returned columns should contain column 'topic'".Observation(
                () => results.Documents[0].Fields.ContainsKey("topic").Should().Be(true));

            "the returned columns should contain column 'surname'".Observation(
                () => results.Documents[0].Fields.ContainsKey("surname").Should().Be(true));

            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }

        #endregion
    }
}