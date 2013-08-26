using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Tests.CSharp.Validator
{
    using FlexSearch.Api.Types;
    using FlexSearch.Validators;

    using NUnit.Framework;

    using ServiceStack.FluentValidation.TestHelper;

    [TestFixture]
    public class AnalyzerValidatorTests
    {
        private AnalyzerValidator analyzerValidator;

        [TestFixtureSetUp]
        public void Init()
        {
            this.analyzerValidator = new AnalyzerValidator(TestHelperFactory.FactoryCollection);
        }

        [Test]
        public void Tokenizer_does_not_exist()
        {
            var sut = new AnalyzerProperties { TokenizerName = "dummy" };
            this.analyzerValidator.ShouldHaveValidationErrorFor(x => x.TokenizerName, sut);
        }


        [Test]
        public void Tokenizer_exists()
        {
            var sut = new AnalyzerProperties { TokenizerName = "standardtokenizer" };
            this.analyzerValidator.ShouldNotHaveValidationErrorFor(x => x.TokenizerName, sut);
        }

        [Test]
        public void StandardTokenizer_is_used_when_no_tokenizer_is_set()
        {
            var sut = new AnalyzerProperties { };
            Assert.AreEqual("standardtokenizer", sut.TokenizerName);
        }

        [Test]
        public void StandardTokenizer_is_used_when_tokenizer_is_set_to_null()
        {
            var sut = new AnalyzerProperties { TokenizerName = null };
            Assert.AreEqual("standardtokenizer", sut.TokenizerName);
        }

        [Test]
        public void StandardTokenizer_is_used_when_tokenizer_is_set_to_blank()
        {
            var sut = new AnalyzerProperties { TokenizerName = " " };
            Assert.AreEqual("standardtokenizer", sut.TokenizerName);
        }
    }
}
