namespace FlexSearch.Tests.CSharp.Validator
{
    using System.Collections;

    using FlexSearch.Validators;

    using NUnit.Framework;

    using ServiceStack.FluentValidation.Results;

    [TestFixture]
    public class PropertyNameValidatorTests
    {
        #region Fields

        private readonly PropertyNameValidator propertyNameValidator = new PropertyNameValidator("test");

        #endregion

        #region Public Methods and Operators

        [Test]
        [TestCaseSource(typeof(TestFactory), "FieldNameCasesInCorrect")]
        public void Fieldname_is_incorrect(string fieldNameValue)
        {
            ValidationResult result = this.propertyNameValidator.Validate("test", fieldNameValue);
            Assert.AreEqual(false, result.IsValid);
        }

        [Test]
        [TestCaseSource(typeof(TestFactory), "FieldNameCasesCorrect")]
        public void Fieldname_should_follow_constraints(string fieldNameValue)
        {
            ValidationResult result = this.propertyNameValidator.Validate("test", fieldNameValue);
            Assert.AreEqual(true, result.IsValid);
        }

        #endregion

        public class TestFactory
        {
            #region Public Properties

            public static IEnumerable FieldNameCasesCorrect
            {
                get
                {
                    yield return new TestCaseData("test").SetName("Field name should be all lower case");
                    yield return new TestCaseData("test121").SetName("Field name can contain numbers");
                }
            }

            public static IEnumerable FieldNameCasesInCorrect
            {
                get
                {
                    yield return new TestCaseData("TEST").SetName("Field name cannot be upper case");
                    yield return new TestCaseData("Test").SetName("Field name cannot contain upper case characters");
                    yield return new TestCaseData("id").SetName("Field name can not be 'id'");
                    yield return new TestCaseData("type").SetName("Field name can not be 'type'");
                    yield return new TestCaseData("lastmodified").SetName("Field name can not be 'lastmodified'");
                }
            }

            #endregion
        }
    }
}