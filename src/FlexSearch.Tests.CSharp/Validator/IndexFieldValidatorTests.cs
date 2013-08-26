namespace FlexSearch.Tests.CSharp.Validator
{
    using System.Collections;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Validators;

    using NUnit.Framework;

    using ServiceStack.FluentValidation.TestHelper;

    [TestFixture]
    public class IndexFieldValidatorTests
    {
        #region Fields

        private IndexFieldValidator indexFieldValidator;

        #endregion

        #region Public Methods and Operators

        [Test]
        [TestCaseSource(typeof(TestFactory), "FieldTypeAnalyzerExistsCases")]
        public void IndexAnalyzer_is_built_for_x_fieldtypes(FieldType fieldType, string indexAnalyzer)
        {
            var sut = new IndexFieldProperties { FieldType = fieldType, IndexAnalyzer = indexAnalyzer };
            this.indexFieldValidator.ShouldNotHaveValidationErrorFor(x => x.IndexAnalyzer, sut);
        }

        [Test]
        [TestCaseSource(typeof(TestFactory), "FieldTypeNonAnalyzerCases")]
        public void IndexAnalyzer_is_ignored_for_x_fieldtypes(FieldType fieldType, string indexAnalyzer)
        {
            var sut = new IndexFieldProperties { FieldType = fieldType, IndexAnalyzer = indexAnalyzer };
            this.indexFieldValidator.ShouldNotHaveValidationErrorFor(x => x.IndexAnalyzer, sut);
        }

        [Test]
        [TestCaseSource(typeof(TestFactory), "FieldTypeAnalyzerNonExistantCases")]
        public void IndexAnalyzer_is_not_ignored_for_x_fieldtypes(FieldType fieldType, string indexAnalyzer)
        {
            var sut = new IndexFieldProperties { FieldType = fieldType, IndexAnalyzer = indexAnalyzer };
            this.indexFieldValidator.ShouldHaveValidationErrorFor(x => x.IndexAnalyzer, sut);
        }

        [TestFixtureSetUp]
        public void Init()
        {
            this.indexFieldValidator = new IndexFieldValidator(
                TestHelperFactory.FactoryCollection,
                new Dictionary<string, AnalyzerProperties>(),
                new Dictionary<string, ScriptProperties>());
        }

        [Test]
        [TestCaseSource(typeof(TestFactory), "FieldTypeAnalyzerExistsCases")]
        public void SearchAnalyzer_is_built_for_x_fieldtypes(FieldType fieldType, string searchAnalyzer)
        {
            var sut = new IndexFieldProperties { FieldType = fieldType, SearchAnalyzer = searchAnalyzer };
            this.indexFieldValidator.ShouldNotHaveValidationErrorFor(x => x.SearchAnalyzer, sut);
        }

        [Test]
        [TestCaseSource(typeof(TestFactory), "FieldTypeNonAnalyzerCases")]
        public void SearchAnalyzer_is_ignored_for_x_fieldtypes(FieldType fieldType, string searchAnalyzer)
        {
            var sut = new IndexFieldProperties { FieldType = fieldType, SearchAnalyzer = searchAnalyzer };
            this.indexFieldValidator.ShouldNotHaveValidationErrorFor(x => x.SearchAnalyzer, sut);
        }

        [Test]
        [TestCaseSource(typeof(TestFactory), "FieldTypeAnalyzerNonExistantCases")]
        public void SearchAnalyzer_is_not_ignored_for_x_fieldtypes(FieldType fieldType, string searchAnalyzer)
        {
            var sut = new IndexFieldProperties { FieldType = fieldType, SearchAnalyzer = searchAnalyzer };
            this.indexFieldValidator.ShouldHaveValidationErrorFor(x => x.SearchAnalyzer, sut);
        }

        #endregion

        public class TestFactory
        {
            #region Public Properties

            public static IEnumerable FieldTypeAnalyzerExistsCases
            {
                get
                {
                    yield return
                        new TestCaseData(FieldType.Custom, "standardanalyzer").SetName(
                            "Analyzer is not ignored for custom field type");
                    yield return
                        new TestCaseData(FieldType.Text, "standardanalyzer").SetName(
                            "Analyzer is ignored for text field type");
                    yield return
                        new TestCaseData(FieldType.Highlight, "standardanalyzer").SetName(
                            "Analyzer is ignored for highlight field type");
                }
            }

            public static IEnumerable FieldTypeAnalyzerNonExistantCases
            {
                get
                {
                    yield return
                        new TestCaseData(FieldType.Custom, "non existing").SetName(
                            "Analyzer is not ignored for custom field type");
                    yield return
                        new TestCaseData(FieldType.Text, "non existing").SetName(
                            "Analyzer is ignored for text field type");
                    yield return
                        new TestCaseData(FieldType.Highlight, "non existing").SetName(
                            "Analyzer is ignored for highlight field type");
                }
            }

            public static IEnumerable FieldTypeNonAnalyzerCases
            {
                get
                {
                    yield return
                        new TestCaseData(FieldType.Bool, "non existing").SetName(
                            "Analyzer is ignored for bool field type");
                    yield return
                        new TestCaseData(FieldType.Date, "non existing").SetName(
                            "Analyzer is ignored for date field type");
                    yield return
                        new TestCaseData(FieldType.DateTime, "non existing").SetName(
                            "Analyzer is ignored for datetime field type");
                    yield return
                        new TestCaseData(FieldType.Double, "non existing").SetName(
                            "Analyzer is ignored for double field type");
                    yield return
                        new TestCaseData(FieldType.ExactText, "non existing").SetName(
                            "Analyzer is ignored for exacttext field type");
                    yield return
                        new TestCaseData(FieldType.Int, "non existing").SetName("Analyzer is ignored for bool int type")
                        ;
                    yield return
                        new TestCaseData(FieldType.Stored, "non existing").SetName(
                            "Analyzer is ignored for stored field type");
                }
            }

            #endregion
        }
    }
}