namespace FlexSearch.Specs.IntegrationTests.Search
{
    using System;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class HighlightingSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [IntegrationAutoFixture]
        public void HighlightingSimpleCasePhraseQuery(Interface.IIndexService indexService, Index index)
        {
            SearchResults results = null;
            string testData = @"
id,topic,abstract
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
";

            "Given an indexservice".Given(() => { });

            "when a new index is created with 2 records, searching for 'practical approach' with highlighting".When(
                () =>
                {
                    index.IndexName = Guid.NewGuid().ToString("N");
                    indexService.AddIndex(index);
                    MockHelpers.AddTestDataToIndex(indexService, index, testData);
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition>
                            {
                                SearchCondition.GetPhraseMatchCondition(
                                    "abstract",
                                    "practical approach")
                            }))
                                      {
                                          Highlight =
                                              new HighlightOption
                                              {
                                                  FragmentsToReturn = 1,
                                                  HighlightedFields = new StringList { "abstract" },
                                                  PreTag = "<imp>",
                                                  PostTag = "</imp>"
                                              }
                                      };
                    results = indexService.PerformQuery(index.IndexName, IndexQuery.NewSearchQuery(searchQuery));
                });

            "it will return 1 result".Observation(() => results.RecordsReturned.Should().Be(1));
            "it will return a highlighted passage".Observation(
                () => results.Documents[0].Highlights.Count.Should().Be(1));
            "the highlighted passage should contain 'practical'".Observation(
                () => results.Documents[0].Highlights[0].Should().Contain("practical"));
            "the highlighted passage should contain 'approach'".Observation(
                () => results.Documents[0].Highlights[0].Should().Contain("approach"));
            "the highlighted passage should contain 'practical' with in pre and post tags".Observation(
                () => results.Documents[0].Highlights[0].Should().Contain("<imp>practical</imp>"));
            "the highlighted passage should contain 'approach' with in pre and post tags".Observation(
                () => results.Documents[0].Highlights[0].Should().Contain("<imp>approach</imp>"));
            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }

        #endregion
    }
}