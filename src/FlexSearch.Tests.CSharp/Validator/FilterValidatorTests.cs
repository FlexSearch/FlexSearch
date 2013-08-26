namespace FlexSearch.Tests.CSharp.Validator
{
    using System.ComponentModel.Composition.Hosting;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Validators;

    using NUnit.Framework;

    using ServiceStack.FluentValidation.TestHelper;

    [TestFixture]
    public class FilterValidatorTests
    {
        #region Fields

        private FilterValidator filterValidator;

        #endregion

        #region Public Methods and Operators

        [Test]
        public void Filtername_should_exist()
        {
            var sut = new Filter { FilterName = "standardfilter" };
            this.filterValidator.ShouldNotHaveValidationErrorFor(x => x.FilterName, sut);
        }

        [Test]
        public void Filter_initialization_should_fail_with_FilterInitError()
        {
            var sut = new Filter { FilterName = "synonymfilter" };
            var result = this.filterValidator.Validate(sut);
            Assert.IsTrue(result.IsValid == false && result.Errors[0].ErrorCode == "FilterInitError");
        }

        [TestFixtureSetUp]
        public void Init()
        {
            this.filterValidator = new FilterValidator(TestHelperFactory.FactoryCollection);
        }

        #endregion
    }
}