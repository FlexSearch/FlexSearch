namespace FlexSearch.Specs.IntegrationTests.Search
{
    using System;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class TermMatchSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [IntegrationAutoFixture]
        public void TermMatchComplexCases(Interface.IIndexService indexService, Index index)
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

            "searching for multiple words will create a new query which will search all the words but not in specific order"
                .Observation(
                    () =>
                    {
                        var searchQuery = new SearchQuery(
                            index.IndexName,
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                {
                                    SearchCondition.GetTermMatchCondition(
                                        "abstract",
                                        "CompSci abbreviated approach")
                                }));
                        SearchResults results = indexService.PerformQuery(
                            index.IndexName,
                            IndexQuery.NewSearchQuery(searchQuery));
                        results.RecordsReturned.Should().Be(1);
                    });

            "searching for multiple words will create a new query which will search all the words using AND style construct but not in specific order"
                .Observation(
                    () =>
                    {
                        var searchQuery = new SearchQuery(
                            index.IndexName,
                            new SearchFilter(
                                FilterType.And,
                                new List<SearchCondition>
                                {
                                    SearchCondition.GetTermMatchCondition(
                                        "abstract",
                                        "CompSci abbreviated approach undefinedword")
                                }));
                        SearchResults results = indexService.PerformQuery(
                            index.IndexName,
                            IndexQuery.NewSearchQuery(searchQuery));
                        results.RecordsReturned.Should().Be(0);
                    });

            "setting 'clausetype' in condition properties can override the default clause construction from AND style to OR"
                .Observation(
                    () =>
                    {
                        SearchCondition condition = SearchCondition.GetTermMatchCondition(
                            "abstract",
                            "CompSci abbreviated approach undefinedword");
                        condition.Parameters.Add("clausetype", "or");

                        var searchQuery = new SearchQuery(
                            index.IndexName,
                            new SearchFilter(FilterType.And, new List<SearchCondition> { condition }));
                        SearchResults results = indexService.PerformQuery(
                            index.IndexName,
                            IndexQuery.NewSearchQuery(searchQuery));
                        results.RecordsReturned.Should().Be(1);
                    });

            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }

        [Thesis]
        [IntegrationAutoFixture]
        public void TermMatchSimpleCases(Interface.IIndexService indexService, Index index)
        {
            string testData = @"
id,givenname,surname,cvv2
1,Aaron,jhonson,23
2,aaron,hewitt,32
3,Fred,Garner,44
4,aaron,Garner,43
5,fred,jhonson,332
";

            "Given an indexservice".Given(() => { });

            "when a new index is created with 5 records".When(
                () =>
                {
                    index.IndexName = Guid.NewGuid().ToString("N");
                    indexService.AddIndex(index);
                    MockHelpers.AddTestDataToIndex(indexService, index, testData);
                });

            "searching for 'id = 1' should return 1 records".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetTermMatchCondition("id", "1") }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(1);
                });

            "searching for int field 'cvv2 = 44' should return 1 records".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetTermMatchCondition("cvv2", "44") }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(1);
                });

            "searching for 'aaron' should return 3 records".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetTermMatchCondition("givenname", "aaron") }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(3);
                });

            "searching for 'aaron' & 'jhonson' should return 1 record".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition>
                            {
                                SearchCondition.GetTermMatchCondition("givenname", "aaron"),
                                SearchCondition.GetTermMatchCondition("surname", "jhonson")
                            }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(1);
                });

            "searching for givenname 'aaron' & surname 'jhonson or Garner' should return 2 record".Observation(
                () =>
                {
                    var searchQuery = new SearchQuery(
                        index.IndexName,
                        new SearchFilter(
                            FilterType.And,
                            new List<SearchCondition> { SearchCondition.GetTermMatchCondition("givenname", "aaron") },
                            new List<SearchFilter>
                            {
                                new SearchFilter(
                                    FilterType.Or,
                                    new List<SearchCondition>
                                    {
                                        SearchCondition
                                            .GetTermMatchCondition(
                                                "surname",
                                                "jhonson"),
                                        SearchCondition
                                            .GetTermMatchCondition(
                                                "surname",
                                                "garner")
                                    })
                            }));
                    SearchResults results = indexService.PerformQuery(
                        index.IndexName,
                        IndexQuery.NewSearchQuery(searchQuery));
                    results.RecordsReturned.Should().Be(2);
                });

            "Clean up".Observation(() => indexService.DeleteIndex(index.IndexName));
        }

        #endregion
    }
}