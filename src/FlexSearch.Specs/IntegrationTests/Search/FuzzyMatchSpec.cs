namespace FlexSearch.Specs.IntegrationTests.Search
{
    using System;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class FuzzyMatchSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [IntegrationAutoFixture]
        public void TermMatchSimpleCases(Interface.IIndexService indexService, Index index)
        {
            string testData = @"
id,givenname,surname,cvv2
1,Aaron,jhonson,23
2,aron,hewitt,32
3,Airon,Garner,44
4,aroon,Garner,43
5,aronn,jhonson,332
6,aroonn,jhonson,332
";

            "Given an indexservice".Given(() => { });

            "when a new index is created with 5 records".When(
                () =>
                {
                    index.IndexName = Guid.NewGuid().ToString("N");
                    indexService.AddIndex(index);
                    MockHelpers.AddTestDataToIndex(indexService, index, testData);
                });

            "searching for 'givenname = aron' with slop of 1 should return 5 records".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetFuzzyMatchCondition("givenname", "aron", 1) }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(5);
                });

            "searching for 'givenname = aron' with slop of 2 should return 6 records".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetFuzzyMatchCondition("givenname", "aron", 2) }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(6);
                });

            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }

        #endregion
    }
}