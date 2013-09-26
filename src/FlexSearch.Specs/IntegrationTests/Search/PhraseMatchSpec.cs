using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Specs.IntegrationTests.Search
{
    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class PhraseMatchSpec
    {
        [Thesis]
        [IntegrationAutoFixture]
        public void PhraseMatchCases(Interface.IIndexService indexService, Index index)
        {
            string testData = @"
id,topic,abstract
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
";

            "Given an indexservice".Given(() => { });

            "when a new index is created with 2 records".When(
                () =>
                {
                    index.IndexName = Guid.NewGuid().ToString("N");
                    indexService.AddIndex(index);
                    MockHelpers.AddTestDataToIndex(indexService, index, testData);
                });

            "searching for 'practical approach' with a slop of 1 will return 1 result".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition>
                            {
                                SearchCondition.GetPhraseMatchCondition(
                                    "abstract",
                                    "practical approach")
                            }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(1);
                });

            "searching for 'approach practical' will not return anything as the order matters".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition>
                            {
                                SearchCondition.GetPhraseMatchCondition(
                                    "abstract",
                                    "approach practical")
                            }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(0);
                });

            // practical approach to computation
            "searching for 'approach computation' with a slop of 2 will return 1 result".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition>
                            {
                                SearchCondition.GetPhraseMatchCondition(
                                    "abstract",
                                    "approach practical",
                                    2)
                            }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(1);
                });

            // comprehensive process that leads
            "searching for 'comprehensive process leads' with a slop of 1 will return 1 result".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition>
                            {
                                SearchCondition.GetPhraseMatchCondition(
                                    "abstract",
                                    "comprehensive process leads",
                                    1)
                            }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(1);
                });

            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }
    }
}
