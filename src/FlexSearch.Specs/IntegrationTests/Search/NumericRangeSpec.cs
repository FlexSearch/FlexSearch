namespace FlexSearch.Specs.IntegrationTests.Search
{
    using System;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class NumericRangeSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [IntegrationAutoFixture]
        public void NumericRangeSimpleCases(Interface.IIndexService indexService, Index index)
        {
            string testData = @"
id,givenname,surname,cvv2
1,Aaron,jhonson,1
2,aaron,hewitt,5
3,Fred,Garner,10
4,aaron,Garner,15
5,fred,jhonson,20
";

            "Given an indexservice".Given(() => { });

            "when a new index is created with 5 records".When(
                () =>
                {
                    index.IndexName = Guid.NewGuid().ToString("N");
                    indexService.AddIndex(index);
                    MockHelpers.AddTestDataToIndex(indexService, index, testData);
                });

            "searching for records with cvv in range 1 to 20 inclusive upper & lower bound should return 5 records"
                .Observation(
                    () =>
                    {
                        var searchQuery = new SearchQuery(
                            index.IndexName,
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                {
                                    SearchCondition.GetNumericRangeCondition(
                                        "cvv2",
                                        "1",
                                        "20",
                                        true,
                                        true)
                                }));
                        SearchResults results = indexService.PerformQuery(
                            index.IndexName,
                            IndexQuery.NewSearchQuery(searchQuery));
                        results.RecordsReturned.Should().Be(5);
                    });

            "searching for records with cvv in range 1 to 20 exclusive upper & lower bound should return 3 records"
                .Observation(
                    () =>
                    {
                        var searchQuery = new SearchQuery(
                            index.IndexName,
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                {
                                    SearchCondition.GetNumericRangeCondition(
                                        "cvv2",
                                        "1",
                                        "20",
                                        false,
                                        false)
                                }));
                        SearchResults results = indexService.PerformQuery(
                            index.IndexName,
                            IndexQuery.NewSearchQuery(searchQuery));
                        results.RecordsReturned.Should().Be(3);
                    });

            "searching for records with cvv in range 1 to 20 inclusive upper & exclusive lower bound should return 4 records"
                .Observation(
                    () =>
                    {
                        var searchQuery = new SearchQuery(
                            index.IndexName,
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                {
                                    SearchCondition.GetNumericRangeCondition(
                                        "cvv2",
                                        "1",
                                        "20",
                                        false,
                                        true)
                                }));
                        SearchResults results = indexService.PerformQuery(
                            index.IndexName,
                            IndexQuery.NewSearchQuery(searchQuery));
                        results.RecordsReturned.Should().Be(4);
                    });

            "searching for records with cvv in range 1 to 20 excluding upper & including lower bound should return 4 records"
                .Observation(
                    () =>
                    {
                        var searchQuery = new SearchQuery(
                            index.IndexName,
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                {
                                    SearchCondition.GetNumericRangeCondition(
                                        "cvv2",
                                        "1",
                                        "20",
                                        true,
                                        false)
                                }));
                        SearchResults results = indexService.PerformQuery(
                            index.IndexName,
                            IndexQuery.NewSearchQuery(searchQuery));
                        results.RecordsReturned.Should().Be(4);
                    });
            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }

        #endregion
    }
}