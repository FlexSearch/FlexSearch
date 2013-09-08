namespace FlexSearch.Tests.CSharp.Analysis
{
    using System.Collections;
    using System.Collections.Generic;

    using FlexSearch.Analysis;
    using FlexSearch.Core;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class FiltersTest
    {
        #region Public Methods and Operators

        [Test]
        [TestCaseSource(typeof(TestFactory), "FilterTestCases")]
        public void Filter_token_generation_Tests(
            Interface.IFlexFilterFactory filterFactory,
            Dictionary<string, string> parameters,
            string parseString,
            List<string> expected)
        {
            filterFactory.Initialize(parameters, new Factories.ResourceLoader());
            var filters = new List<Interface.IFlexFilterFactory> { filterFactory };
            var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
            List<string> actual = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", parseString);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void KeepWord_filter_should_only_keep_keepwords()
        {
            Interface.IFlexFilterFactory filter = new Filters.KeepWordsFilterFactory();
            var mock = new Mock<Interface.IResourceLoader>();
            mock.Setup(x => x.LoadResourceAsList("test.txt")).Returns(new List<string> { "hello", "world" });

            filter.Initialize(new Dictionary<string, string> { { "filename", "test.txt" } }, mock.Object);

            var filters = new List<Interface.IFlexFilterFactory> { filter };
            var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
            List<string> actual = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "hello world test");
            Assert.AreEqual(new List<string> { "hello", "world" }, actual);
        }

        [Test]
        public void StopWord_filter_should_remove_stopwords()
        {
            Interface.IFlexFilterFactory filter = new Filters.StopFilterFactory();
            var mock = new Mock<Interface.IResourceLoader>();
            mock.Setup(x => x.LoadResourceAsList("test.txt")).Returns(new List<string> { "hello", "world" });

            filter.Initialize(new Dictionary<string, string> { { "filename", "test.txt" } }, mock.Object);

            var filters = new List<Interface.IFlexFilterFactory> { filter };
            var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
            List<string> actual = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "hello world test");
            Assert.AreEqual(new List<string> { "test" }, actual);
        }

        [Test]
        public void Synonym_filter_should_generate_synonym()
        {
            Interface.IFlexFilterFactory filter = new Filters.SynonymFilter();
            var mock = new Mock<Interface.IResourceLoader>();
            mock.Setup(x => x.LoadResourceAsMap("test.txt"))
                .Returns(new List<string[]> { { new[] { "easy", "simple", "clear" } } });

            filter.Initialize(new Dictionary<string, string> { { "filename", "test.txt" } }, mock.Object);

            var filters = new List<Interface.IFlexFilterFactory> { filter };
            var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
            List<string> actual = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "easy");
            Assert.AreEqual(new List<string> { "easy", "simple", "clear" }, actual);
        }

        #endregion

        public class TestFactory
        {
            #region Public Properties

            public static IEnumerable FilterTestCases
            {
                get
                {
                    yield return
                        new TestCaseData(
                            new Filters.StandardFilterFactory(),
                            new Dictionary<string, string>(),
                            "Bob's I.O.U.",
                            new List<string> { "Bob's", "I.O.U" }).SetName("Standard Filter");

                    yield return
                        new TestCaseData(
                            new Filters.LowerCaseFilterFactory(),
                            new Dictionary<string, string>(),
                            "Bob's I.O.U.",
                            new List<string> { "bob's", "i.o.u" }).SetName("Lower case Filter");

                    yield return
                        new TestCaseData(
                            new Filters.LengthFilterFactory(),
                            new Dictionary<string, string> { { "min", "3" }, { "max", "7" } },
                            "turn right at Albuquerque",
                            new List<string> { "turn", "right" }).SetName("Length Filter");

                    yield return
                        new TestCaseData(
                            new Filters.PatternReplaceFilterFactory(),
                            new Dictionary<string, string> { { "pattern", "cat" }, { "replacementtext", "dog" } },
                            "cat concatenate catycat",
                            new List<string> { "dog", "condogenate", "dogydog" }).SetName("Pattern replace Filter");

                    yield return
                        new TestCaseData(
                            new Filters.ReverseStringFilterFactory(),
                            new Dictionary<string, string> { },
                            "hello how are you",
                            new List<string> { "olleh", "woh", "era", "uoy" }).SetName("Pattern replace Filter");
                }
            }

            #endregion
        }
    }
}