namespace FlexSearch.Specs.IntegrationTests.Index
{
    using System;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class DynamicFieldSpecs
    {
        #region Public Methods and Operators

        [Thesis]
        [IntegrationAutoFixture]
        public void DynamicFieldGeneration(Interface.IIndexService indexService, Index index)
        {
            string testData = @"
id,topic,givenname,surname,cvv2
1,a,aron,jhonson,1
2,c,steve,hewitt,1
3,b,george,Garner,1
4,e,jhon,Garner,1
5,d,simon,jhonson,1
";
            SearchResults results = null;

            "Given an indexservice".Given(() => { });

            "when a new index is created with 5 records".When(
                () =>
                {
                    index.IndexName = Guid.NewGuid().ToString("N");
                    indexService.AddIndex(index);
                    MockHelpers.AddTestDataToIndex(indexService, index, testData);
                });

            "searching is possible on the dynamic field".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        new StringList { "fullname" },
                        index.IndexName,
                        10,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition>
                            {
                                SearchCondition.GetPhraseMatchCondition(
                                    "fullname",
                                    "aron jhonson")
                            }));
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                    results.Documents[0].Fields["fullname"].Should().Be("aron jhonson");
                });

            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }

        #endregion
    }
}