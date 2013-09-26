namespace FlexSearch.Specs.IntegrationTests.Search
{
    using System;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class PagingSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [IntegrationAutoFixture]
        public void PagingSimpleCase(Interface.IIndexService indexService, Index index)
        {
            SearchResults results = null;

            string testData = @"
id,givenname,surname,cvv2
1,Aaron,jhonson,1
2,aron,hewitt,1
3,Airon,Garner,1
4,aroon,Garner,1
5,aronn,jhonson,1
6,aroonn,jhonson,1
";

            "Given an index with 6 records".Given(
                () =>
                {
                    index.IndexName = Guid.NewGuid().ToString("N");
                    indexService.AddIndex(index);
                    MockHelpers.AddTestDataToIndex(indexService, index, testData);
                });

            "searching for 'cvv2 = 1' with records to return = 2".When(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetTermMatchCondition("cvv2", "1") }))
                                      {
                                          Count
                                              =
                                              2
                                      };
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                });

            "will return 2 records".Observation(() => results.RecordsReturned.Should().Be(2));

            "first record will be with id = 1".Observation(() => results.Documents[0].Id.Should().Be("1"));

            "second record will be with id = 2".Observation(() => results.Documents[1].Id.Should().Be("2"));

            "searching for 'cvv2 = 1' with records to return = 2 and skip = 2".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetTermMatchCondition("cvv2", "1") }))
                    {
                        Count = 2, Skip = 2
                    };
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                });

            "will return 2 records".Observation(() => results.RecordsReturned.Should().Be(2));
            "first record will be with id = 3".Observation(() => results.Documents[0].Id.Should().Be("3"));
            "second record will be with id = 4".Observation(() => results.Documents[1].Id.Should().Be("4"));


            "searching for 'cvv2 = 1' with records to return = 2 and skip = 3".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetTermMatchCondition("cvv2", "1") }))
                    {
                        Count = 2,
                        Skip = 3
                    };
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                });

            "will return 2 records".Observation(() => results.RecordsReturned.Should().Be(2));
            "first record will be with id = 4".Observation(() => results.Documents[0].Id.Should().Be("4"));
            "second record will be with id = 5".Observation(() => results.Documents[1].Id.Should().Be("5"));


            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }

        #endregion
    }
}