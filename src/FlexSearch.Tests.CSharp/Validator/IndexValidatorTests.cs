namespace FlexSearch.Tests.CSharp.Validator
{
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Validators;

    using NUnit.Framework;

    [TestFixture]
    public class IndexValidatorTests
    {
        #region Fields

        private IndexValidator indexValidator;

        #endregion

        #region Public Methods and Operators

        [TestFixtureSetUp]
        public void Init()
        {
            this.indexValidator = new IndexValidator(TestHelperFactory.FactoryCollection, new IndexValidationParameters(true));
        }

        [Test]
        public void Simple_settings_should_get_validated()
        {
            var sut = new Index();
            sut.IndexName = "contact";
            sut.Analyzers.Add(
                "test",
                new AnalyzerProperties { Filters = new List<Filter> { new Filter { FilterName = "standardfilter" } } });
            sut.Fields.Add("firstname", new IndexFieldProperties { FieldType = FieldType.Text });
            sut.Fields.Add("fullname", new IndexFieldProperties { FieldType = FieldType.Text, ScriptName = "fullname" });
            sut.Scripts.Add(
                "fullname",
                new ScriptProperties
                {
                    ScriptOption = ScriptOption.SingleLine,
                    ScriptType = ScriptType.ComputedField,
                    ScriptSource = "fields[\"givenname\"] + \" \" + fields[\"surname\"]"
                });

            var result = this.indexValidator.Validate(sut);
            Assert.AreEqual(true, result.IsValid);
        }

        #endregion
    }
}