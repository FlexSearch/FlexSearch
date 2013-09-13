namespace FlexSearch.Tests.CSharp.Validator
{
    using FlexSearch.Api.Types;
    using FlexSearch.Validators;

    using NUnit.Framework;

    [TestFixture]
    public class AnalyzerValidatorTests
    {
        #region Fields

        private AnalyzerValidator analyzerValidator;

        #endregion

        #region Public Methods and Operators

        [TestFixtureSetUp]
        public void Init()
        {
            this.analyzerValidator = new AnalyzerValidator(TestHelperFactory.FactoryCollection);
        }

        [Test]
        public void StandardTokenizer_is_used_when_no_tokenizer_is_set()
        {
            var sut = new AnalyzerProperties { };
            Assert.AreEqual("standardtokenizer", sut.Tokenizer.TokenizerName);
        }

        [Test]
        public void StandardTokenizer_is_used_when_tokenizer_is_set_to_blank()
        {
            var sut = new AnalyzerProperties { Tokenizer = new Tokenizer { TokenizerName = " " } };
            Assert.AreEqual("standardtokenizer", sut.Tokenizer.TokenizerName);
        }

        [Test]
        public void StandardTokenizer_is_used_when_tokenizer_is_set_to_null()
        {
            var sut = new AnalyzerProperties { Tokenizer = new Tokenizer { TokenizerName = null } };
            Assert.AreEqual("standardtokenizer", sut.Tokenizer.TokenizerName);
        }

        [Test]
        public void Tokenizer_does_not_exist()
        {
            var sut = new AnalyzerProperties { Tokenizer = new Tokenizer { TokenizerName = "dummy" } };
            var res = this.analyzerValidator.Validate(sut);
            Assert.AreEqual(false, res.IsValid);
        }

        [Test]
        public void Tokenizer_exists()
        {
            var sut = new AnalyzerProperties { Tokenizer = new Tokenizer { TokenizerName = "standardtokenizer" } };
            var res = this.analyzerValidator.Validate(sut);
            Assert.AreEqual(true, res.IsValid);
        }

        #endregion
    }
}