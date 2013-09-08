namespace FlexSearch.Tests.CSharp.Analysis
{
    using System.Collections;
    using System.Collections.Generic;

    using FlexSearch.Analysis;
    using FlexSearch.Core;

    using NUnit.Framework;

    [TestFixture]
    public class TokenizersTest
    {
        #region Public Methods and Operators

        [Test]
        [TestCaseSource(typeof(TestFactory), "ToenizerTestCases")]
        public void Tokenizer_token_generation_Tests(
            Interface.IFlexTokenizerFactory tokenizerFactory,
            string parseString,
            List<string> expected)
        {
            // Creating a dummy filter which won't do anything so that we can test the effect of tokenizer 
            // in a stand alone manner
            Interface.IFlexFilterFactory filter = new Filters.PatternReplaceFilterFactory();
            filter.Initialize(new Dictionary<string, string> { { "pattern", "1" }, { "replacementtext", "" } }, new Factories.ResourceLoader());
            var filters = new List<Interface.IFlexFilterFactory> { filter };
            var analyzer = new CustomAnalyzer(tokenizerFactory, filters.ToArray());
            List<string> actual = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", parseString);
            Assert.AreEqual(expected, actual);
        }

        #endregion

        public class TestFactory
        {
            #region Public Properties

            public static IEnumerable ToenizerTestCases
            {
                get
                {
                    yield return
                        new TestCaseData(
                            new Tokenizers.StandardTokenizerFactory(),
                            "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                            new List<string>
                            {
                                "Please",
                                "email",
                                "john.doe",
                                "foo.com",
                                "by",
                                "03",
                                "09",
                                "re",
                                "m37",
                                "xq"
                            }).SetName("Standard Tokenizer");

                    yield return
                        new TestCaseData(
                            new Tokenizers.ClassicTokenizerFactory(),
                            "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                            new List<string> { "Please", "email", "john.doe@foo.com", "by", "03-09", "re", "m37-xq" })
                            .SetName("Classic Tokenizer");

                    yield return
                        new TestCaseData(
                            new Tokenizers.UAX29URLEmailTokenizerFactory(),
                            "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                            new List<string>
                            {
                                "Please",
                                "email",
                                "john.doe@foo.com",
                                "by",
                                "03",
                                "09",
                                "re",
                                "m37",
                                "xq"
                            }).SetName("UAX29URLEmail Tokenizer");

                    yield return
                        new TestCaseData(
                            new Tokenizers.KeywordTokenizerFactory(),
                            "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                            new List<string> { "Please, email john.doe@foo.com by 03-09, re: m37-xq." }).SetName(
                                "Keyword Tokenizer");

                    yield return
                        new TestCaseData(
                            new Tokenizers.LowercaseTokenizerFactory(),
                            "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                            new List<string> { "please", "email", "john", "doe", "foo", "com", "by", "re", "m", "xq" })
                            .SetName("Lowercase Tokenizer");

                    yield return
                        new TestCaseData(
                            new Tokenizers.LetterTokenizerFactory(),
                            "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                            new List<string> { "Please", "email", "john", "doe", "foo", "com", "by", "re", "m", "xq" })
                            .SetName("Lowercase Tokenizer");
                    
                    yield return
                        new TestCaseData(
                            new Tokenizers.WhitespaceTokenizerFactory(),
                            "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                            new List<string> { "Please,", "email", "john.doe@foo.com", "by", "03-09,", "re:", "m37-xq." })
                            .SetName("Lowercase Tokenizer");
                }
            }

            #endregion
        }
    }
}