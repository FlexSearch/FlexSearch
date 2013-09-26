namespace FlexSearch.Specs.UnitTests.Domain
{
    using FlexSearch.Api.Types;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;
    using FlexSearch.Validators;

    using FluentAssertions;

    using ServiceStack.FluentValidation.TestHelper;

    public class IndexFieldSpec
    {
        #region Public Methods and Operators

        [Specification]
        public void DefaultValueTest()
        {
            IndexFieldProperties sut = null;
            "Given a new index field properties".Given(() => sut = new IndexFieldProperties());

            "'standardanalyzer' should be the default 'SearchAnalyzer'".Then(() => sut.SearchAnalyzer.Should().Be("standardanalyzer"));
            "'standardanalyzer' should be the default 'IndexAnalyzer'".Then(() => sut.IndexAnalyzer.Should().Be("standardanalyzer"));
            "'analyze' should be true".Then(() => sut.Analyze.Should().BeTrue());
            "'store' should be true".Then(() => sut.Store.Should().BeTrue());
            "'index' should be true".Then(() => sut.Index.Should().BeTrue());
            "'FieldType' should be 'Text'".Then(() => sut.FieldType.Should().Be(FieldType.Text));
            "'FieldTermVector' should be 'StoreTermVectorsWithPositionsandOffsets'".Then(() => sut.FieldTermVector.Should().Be(FieldTermVector.StoreTermVectorsWithPositionsandOffsets));
        }

        [Thesis]
        [UnitInlineAutoFixture(FieldType.Bool, "dummy")]
        [UnitInlineAutoFixture(FieldType.Date, "dummy")]
        [UnitInlineAutoFixture(FieldType.DateTime, "dummy")]
        [UnitInlineAutoFixture(FieldType.Double, "dummy")]
        [UnitInlineAutoFixture(FieldType.ExactText, "dummy")]
        [UnitInlineAutoFixture(FieldType.Int, "dummy")]
        [UnitInlineAutoFixture(FieldType.Stored, "dummy")]
        public void AnalyzersAreIgnoredForXFieldtypes(
            FieldType fieldType,
            string analyzer,
            IndexFieldProperties indexFieldProperties,
            IndexFieldValidator validator)
        {
            "Given an index field validator and index field properties".Given(
                () =>
                {
                    indexFieldProperties.FieldType = fieldType;
                    indexFieldProperties.IndexAnalyzer = analyzer;
                    indexFieldProperties.SearchAnalyzer = analyzer;
                });

            string.Format(
                "when a field of type '{0}' is validated with 'IndexAnalyzer' & SearchAnalyzer value of '{1}'",
                fieldType,
                analyzer).When(() => { });

            "then there should be no validation error for 'IndexAnalyzer'".Then(
                () => validator.ShouldNotHaveValidationErrorFor(x => x.IndexAnalyzer, indexFieldProperties));

            "then there should be no validation error for 'SearchAnalyzer'".Then(
                () => validator.ShouldNotHaveValidationErrorFor(x => x.SearchAnalyzer, indexFieldProperties));
        }

        [Thesis]
        [UnitInlineAutoFixture(FieldType.Text, "test")]
        [UnitInlineAutoFixture(FieldType.Highlight, "test")]
        [UnitInlineAutoFixture(FieldType.Custom, "test")]
        public void CorrectAnalyzersShouldBeSpecifiedForXFieldtypes(
            FieldType fieldType,
            string analyzer,
            IndexFieldProperties indexFieldProperties,
            IndexFieldValidator validator)
        {
            "Given an index field validator and index field properties".Given(
                () =>
                {
                    indexFieldProperties.FieldType = fieldType;
                    indexFieldProperties.IndexAnalyzer = analyzer;
                    indexFieldProperties.SearchAnalyzer = analyzer;
                });

            string.Format(
                "when a field of type '{0}' is validated with 'IndexAnalyzer' & SearchAnalyzer value of '{1}' which is not correct",
                fieldType,
                analyzer).When(() => { });

            "then there should be validation error for 'IndexAnalyzer'".Then(
                () => validator.ShouldHaveValidationErrorFor(x => x.IndexAnalyzer, indexFieldProperties));

            "then there should be validation error for 'SearchAnalyzer'".Then(
                () => validator.ShouldHaveValidationErrorFor(x => x.SearchAnalyzer, indexFieldProperties));
        }

        [Thesis]
        [UnitInlineAutoFixture(FieldType.Text, "standardanalyzer")]
        [UnitInlineAutoFixture(FieldType.Highlight, "standardanalyzer")]
        [UnitInlineAutoFixture(FieldType.Custom, "standardanalyzer")]
        public void CorrectAnalyzersShouldBeSpecifiedForXFieldtypes1(
            FieldType fieldType,
            string analyzer,
            IndexFieldProperties indexFieldProperties,
            IndexFieldValidator validator)
        {
            "Given an index field validator and index field properties".Given(
                () =>
                {
                    indexFieldProperties.FieldType = fieldType;
                    indexFieldProperties.IndexAnalyzer = analyzer;
                    indexFieldProperties.SearchAnalyzer = analyzer;
                });

            string.Format(
                "when a field of type '{0}' is validated with 'IndexAnalyzer' & SearchAnalyzer value of '{1}' which is correct",
                fieldType,
                analyzer).When(() => { });

            "then there should be no validation error for 'IndexAnalyzer'".Then(
                () => validator.ShouldNotHaveValidationErrorFor(x => x.IndexAnalyzer, indexFieldProperties));

            "then there should be no validation error for 'SearchAnalyzer'".Then(
                () => validator.ShouldNotHaveValidationErrorFor(x => x.SearchAnalyzer, indexFieldProperties));
        }

        #endregion
    }
}