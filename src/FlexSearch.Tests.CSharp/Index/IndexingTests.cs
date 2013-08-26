namespace FlexSearch.Tests.CSharp.Index
{
    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using NUnit.Framework;

    [TestFixture]
    public class IndexingTests
    {
        #region Fields

        private Interface.IIndexService indexService;

        #endregion

        #region Public Methods and Operators

        [Test]
        public void Dynamically_generated_fields_are_searchable()
        {
            var searchQuery = new SearchQuery(
                new[] { "fullname" },
                "contact",
                10,
                new SearchFilter(
                    FilterType.And,
                    new[]
                    {
                        new SearchCondition(
                            "fullname",
                            "term_match",
                            new[]
                            {
                                TestDataFactory.GetContactTestData()[0].GivenName + " "
                                + TestDataFactory.GetContactTestData()[0].Surname
                            })
                    }));
            SearchResults results = this.indexService.PerformQuery("contact", IndexQuery.NewSearchQuery(searchQuery));
            Assert.AreEqual(
                TestDataFactory.GetContactTestData()[0].GivenName + " "
                + TestDataFactory.GetContactTestData()[0].Surname,
                results.Documents[0].Fields["fullname"]);
        }

        [Test]
        public void Fullname_is_generated_dynamically()
        {
            var searchQuery = new SearchQuery(
                new[] { "fullname" },
                "contact",
                10,
                new SearchFilter(FilterType.And, new[] { new SearchCondition("id", "term_match", new[] { "1" }) }));
            SearchResults results = this.indexService.PerformQuery("contact", IndexQuery.NewSearchQuery(searchQuery));
            Assert.AreEqual(
                TestDataFactory.GetContactTestData()[0].GivenName + " "
                + TestDataFactory.GetContactTestData()[0].Surname,
                results.Documents[0].Fields["fullname"]);
        }

        [TestFixtureSetUp]
        public void Init()
        {
            this.indexService = TestHelperFactory.GetDefaultIndexService();
            if (!this.indexService.IndexExists("contact"))
            {
                Index settings = TestHelperFactory.GetBasicIndexSettingsForContact();
                this.indexService.AddIndex(settings);
                TestDataFactory.PopulateIndexWithTestData(this.indexService);
            }
        }

        #endregion
    }
}