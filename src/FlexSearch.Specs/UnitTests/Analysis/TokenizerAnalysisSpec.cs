namespace FlexSearch.Specs.UnitTests.Analysis
{
    using System.Collections.Generic;

    using FlexSearch.Analysis;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    using Xunit.Extensions;

    public class TokenizerAnalysisSpec
    {
        #region Public Properties

        public static IEnumerable<object[]> TokenizerTestCases
        {
            get
            {
                return new[]
                       {
                           new object[]
                           {
                               "Standard Tokenizer", new Tokenizers.StandardTokenizerFactory(),
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
                               }
                           },
                           new object[]
                           {
                               "Classic Tokenizer", new Tokenizers.ClassicTokenizerFactory(),
                               "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                               new List<string> { "Please", "email", "john.doe@foo.com", "by", "03-09", "re", "m37-xq" }
                           },
                           new object[]
                           {
                               "UAX29URLEmail Tokenizer", new Tokenizers.UAX29URLEmailTokenizerFactory(),
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
                               }
                           },
                           new object[]
                           {
                               "Keyword Tokenizer", new Tokenizers.KeywordTokenizerFactory(),
                               "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                               new List<string> { "Please, email john.doe@foo.com by 03-09, re: m37-xq." }
                           },
                           new object[]
                           {
                               "Lowercase Tokenizer", new Tokenizers.LowercaseTokenizerFactory(),
                               "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                               new List<string> { "please", "email", "john", "doe", "foo", "com", "by", "re", "m", "xq" }
                           },
                           new object[]
                           {
                               "Letter Tokenizer", new Tokenizers.LetterTokenizerFactory(),
                               "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                               new List<string> { "Please", "email", "john", "doe", "foo", "com", "by", "re", "m", "xq" }
                           },
                           new object[]
                           {
                               "Whitespace Tokenizer", new Tokenizers.WhitespaceTokenizerFactory(),
                               "Please, email john.doe@foo.com by 03-09, re: m37-xq.",
                               new List<string>
                               {
                                   "Please,",
                                   "email",
                                   "john.doe@foo.com",
                                   "by",
                                   "03-09,",
                                   "re:",
                                   "m37-xq."
                               }
                           }
                       };
            }
        }

        #endregion

        #region Public Methods and Operators

        [Thesis]
        [PropertyData("TokenizerTestCases")]
        public void GenericTokenizerTests(
            string tokenizerName,
            Interface.IFlexTokenizerFactory tokenizerFactory,
            string parseString,
            List<string> expected)
        {
            List<string> result = null;
            (string.Format("Given a {0}", tokenizerName)).Given(() => { });
            (string.Format("when a sample text {0} is analyzed", parseString)).When(
                () =>
                {
                    // Creating a dummy filter which won't do anything so that we can test the effect of tokenizer 
                    // in a stand alone manner
                    Interface.IFlexFilterFactory filter = new Filters.PatternReplaceFilterFactory();
                    filter.Initialize(
                        new Dictionary<string, string> { { "pattern", "1" }, { "replacementtext", "" } },
                        new Factories.ResourceLoader());
                    var filters = new List<Interface.IFlexFilterFactory> { filter };
                    var analyzer = new CustomAnalyzer(tokenizerFactory, filters.ToArray());
                    result = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", parseString);
                });

            string.Format("it should produce {0} tokens", expected.Count)
                .Observation(() => result.Count.Should().Be(expected.Count));
            string.Format("it should be '{0}'", string.Join("',", expected)).Observation(() => result.Should().Equal(expected));
        }

        #endregion
    }
}