namespace FlexSearch.Specs.UnitTests.Analysis
{
    using System.Collections.Generic;

    using FlexSearch.Analysis;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class FilterAnalysisSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [UnitAutoFixture]
        public void KeepWordFilterShouldOnlyKeepKeepwords(
            Filters.KeepWordsFilterFactory sut,
            Interface.IResourceLoader resourceLoader)
        {
            List<string> result = null;

            "Given a keepword filter".Given(() => { });
            "when a wordlist of keepwords is passed and a sample text 'hello world test' is analyzed".When(
                () =>
                {
                    ((Interface.IFlexFilterFactory)sut).Initialize(
                        new Dictionary<string, string> { { "filename", "wordlist.txt" } },
                        resourceLoader);
                    var filters = new List<Interface.IFlexFilterFactory> { sut };
                    var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
                    result = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "hello world test");
                });

            "it should produce 2 tokens".Observation(() => result.Count.Should().Be(2));
            "it should remove all non keep words from the input".Then(
                () => result.Should().Equal(new List<string> { "hello", "world" }));
        }

        [Thesis]
        [UnitAutoFixture]
        public void StandardFilterTests(Filters.StandardFilterFactory sut, Interface.IResourceLoader resourceLoader)
        {
            List<string> result = null;

            "Given a standard filter".Given(() => { });
            "when a sample text 'Bob's I.O.U.' is analyzed".When(
                () =>
                {
                    ((Interface.IFlexFilterFactory)sut).Initialize(new Dictionary<string, string>(), resourceLoader);
                    var filters = new List<Interface.IFlexFilterFactory> { sut };
                    var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
                    result = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "Bob's I.O.U.");
                });

            "it should produce 2 tokens".Observation(() => result.Count.Should().Be(2));
            "it should be 'Bob's','I.O.U'".Observation(
                () => result.Should().Equal(new List<string> { "Bob's", "I.O.U" }));
        }

        [Thesis]
        [UnitAutoFixture]
        public void LowerCaseFilterTests(Filters.LowerCaseFilterFactory sut, Interface.IResourceLoader resourceLoader)
        {
            List<string> result = null;

            "Given a LowerCase filter".Given(() => { });
            "when a sample text 'Bob's I.O.U.' is analyzed".When(
                () =>
                {
                    ((Interface.IFlexFilterFactory)sut).Initialize(new Dictionary<string, string>(), resourceLoader);
                    var filters = new List<Interface.IFlexFilterFactory> { sut };
                    var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
                    result = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "Bob's I.O.U.");
                });

            "it should produce 2 tokens".Observation(() => result.Count.Should().Be(2));
            "it should be 'bob's','i.o.u'".Observation(
                () => result.Should().Equal(new List<string> { "bob's", "i.o.u" }));
        }

        [Thesis]
        [UnitAutoFixture]
        public void LengthFilterTests(Filters.LengthFilterFactory sut, Interface.IResourceLoader resourceLoader)
        {
            List<string> result = null;

            "Given a Length Filter".Given(() => { });
            "when a sample text 'turn right at Albuquerque' is analyzed with min:3 and max:7".When(
                () =>
                {
                    ((Interface.IFlexFilterFactory)sut).Initialize(new Dictionary<string, string> { { "min", "3" }, { "max", "7" } }, resourceLoader);
                    var filters = new List<Interface.IFlexFilterFactory> { sut };
                    var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
                    result = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "turn right at Albuquerque");
                });

            "it should produce 2 tokens".Observation(() => result.Count.Should().Be(2));
            "it should be 'turn','right'".Observation(
                () => result.Should().Equal(new List<string> { "turn", "right" }));
        }

        [Thesis]
        [UnitAutoFixture]
        public void PatternReplaceTests(Filters.PatternReplaceFilterFactory sut, Interface.IResourceLoader resourceLoader)
        {
            List<string> result = null;

            "Given a PatternReplace Filter".Given(() => { });
            "when a sample text 'cat concatenate catycat' is analyzed with pattern:cat and replacementtext:dog".When(
                () =>
                {
                    ((Interface.IFlexFilterFactory)sut).Initialize(new Dictionary<string, string> { { "pattern", "cat" }, { "replacementtext", "dog" } }, resourceLoader);
                    var filters = new List<Interface.IFlexFilterFactory> { sut };
                    var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
                    result = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "cat concatenate catycat");
                });

            "it should produce 3 tokens".Observation(() => result.Count.Should().Be(3));
            "it should be 'turn','right'".Observation(
                () => result.Should().Equal(new List<string> { "dog", "condogenate", "dogydog" }));
        }

        [Thesis]
        [UnitAutoFixture]
        public void ReverseStringTests(Filters.ReverseStringFilterFactory sut, Interface.IResourceLoader resourceLoader)
        {
            List<string> result = null;

            "Given a ReverseString Filter".Given(() => { });
            "when a sample text 'hello how are you' is analyzed".When(
                () =>
                {
                    ((Interface.IFlexFilterFactory)sut).Initialize(new Dictionary<string, string>(), resourceLoader);
                    var filters = new List<Interface.IFlexFilterFactory> { sut };
                    var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
                    result = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "hello how are you");
                });

            "it should produce 4 tokens".Observation(() => result.Count.Should().Be(4));
            "it should be 'olleh', 'woh', 'era', 'uoy' ".Observation(
                () => result.Should().Equal(new List<string> { "olleh", "woh", "era", "uoy" }));
        }

        [Thesis]
        [UnitAutoFixture]
        public void StopWordFilterShouldRemoveStopwords(
            Filters.StopFilterFactory sut,
            Interface.IResourceLoader resourceLoader)
        {
            List<string> result = null;

            "Given a stopword filter".Given(() => { });
            "when a wordlist of stopwords is passed and a sample text 'hello world test' is analyzed".When(
                () =>
                {
                    ((Interface.IFlexFilterFactory)sut).Initialize(
                        new Dictionary<string, string> { { "filename", "wordlist.txt" } },
                        resourceLoader);
                    var filters = new List<Interface.IFlexFilterFactory> { sut };
                    var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
                    result = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "hello world test");
                });

            "it should produce 1 token".Observation(() => result.Count.Should().Be(1));
            "it should remove all the stopwords from the input".Then(
                () => result.Should().Equal(new List<string> { "test" }));
        }

        [Thesis]
        [UnitAutoFixture]
        public void SynonymFilterShouldGenerateSynonym(
            Filters.SynonymFilter sut,
            Interface.IResourceLoader resourceLoader)
        {
            List<string> result = null;

            "Given a Synonym filter".Given(() => { });
            "when a wordlist of Synonym is passed and a sample text 'easy' is analyzed".When(
                () =>
                {
                    ((Interface.IFlexFilterFactory)sut).Initialize(
                        new Dictionary<string, string> { { "filename", "synonymlist.txt" } },
                        resourceLoader);
                    var filters = new List<Interface.IFlexFilterFactory> { sut };
                    var analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters.ToArray());
                    result = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", "easy");
                });

            "it should produce 3 tokens".Observation(() => result.Count.Should().Be(3));
            "it should generate new tokens for the synonmyns".Then(
                () => result.Should().Equal(new List<string> { "easy", "simple", "clear" }));
        }

        #endregion
    }
}