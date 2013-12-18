namespace FlexSearch.Specs.UnitTests.Domain
{
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    using Xunit;

    public class IndexFieldSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [UnitInlineAutoFixture(Api.FieldType.Bool, "dummy")]
        [UnitInlineAutoFixture(Api.FieldType.Date, "dummy")]
        [UnitInlineAutoFixture(Api.FieldType.DateTime, "dummy")]
        [UnitInlineAutoFixture(Api.FieldType.Double, "dummy")]
        [UnitInlineAutoFixture(Api.FieldType.ExactText, "dummy")]
        [UnitInlineAutoFixture(Api.FieldType.Int, "dummy")]
        [UnitInlineAutoFixture(Api.FieldType.Stored, "dummy")]
        public void AnalyzersAreIgnoredForXFieldtypes(
            Api.FieldType fieldType,
            string analyzer,
            Api.IndexFieldProperties indexFieldProperties,
            Interface.IFactoryCollection factory)
        {
            "Given an index field validator and index field properties".Given(
                () =>
                {
                    indexFieldProperties.ScriptName = string.Empty;
                    indexFieldProperties.FieldType = fieldType;
                });

            string.Format(
                "when a field of type '{0}' is validated with 'IndexAnalyzer' & SearchAnalyzer value of '{1}'",
                fieldType,
                analyzer).When(() => { });

            "then there should be no validation error for 'IndexAnalyzer'".Then(
                () =>
                {
                    indexFieldProperties.IndexAnalyzer = analyzer;
                    Assert.DoesNotThrow(
                        () =>
                            Validator.IndexFieldValidator(
                                factory,
                                new Api.AnalyzerDictionary(),
                                new Api.ScriptDictionary(),
                                "",
                                indexFieldProperties));
                });

            "then there should be no validation error for 'SearchAnalyzer'".Then(
                () =>
                {
                    indexFieldProperties.SearchAnalyzer = analyzer;
                    Assert.DoesNotThrow(
                        () =>
                            Validator.IndexFieldValidator(
                                factory,
                                new Api.AnalyzerDictionary(),
                                new Api.ScriptDictionary(),
                                "",
                                indexFieldProperties));
                });
        }

        [Thesis]
        [UnitInlineAutoFixture(Api.FieldType.Text, "test")]
        [UnitInlineAutoFixture(Api.FieldType.Highlight, "test")]
        [UnitInlineAutoFixture(Api.FieldType.Custom, "test")]
        public void CorrectAnalyzersShouldBeSpecifiedForXFieldtypes(
            Api.FieldType fieldType,
            string analyzer,
            Api.IndexFieldProperties indexFieldProperties,
            Interface.IFactoryCollection factory)
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
                 () =>
                 {
                     indexFieldProperties.IndexAnalyzer = analyzer;
                     Assert.Throws<Validator.ValidationException>(
                         () =>
                             Validator.IndexFieldValidator(
                                 factory,
                                 new Api.AnalyzerDictionary(),
                                 new Api.ScriptDictionary(),
                                 "",
                                 indexFieldProperties));
                 });

            "then there should be validation error for 'SearchAnalyzer'".Then(
                () =>
                {
                    indexFieldProperties.SearchAnalyzer = analyzer;
                    Assert.Throws<Validator.ValidationException>(
                        () =>
                            Validator.IndexFieldValidator(
                                factory,
                                new Api.AnalyzerDictionary(),
                                new Api.ScriptDictionary(),
                                "",
                                indexFieldProperties));
                });
        }

        [Thesis]
        [UnitInlineAutoFixture(Api.FieldType.Text, "standardanalyzer")]
        [UnitInlineAutoFixture(Api.FieldType.Highlight, "standardanalyzer")]
        [UnitInlineAutoFixture(Api.FieldType.Custom, "standardanalyzer")]
        public void CorrectAnalyzersShouldBeSpecifiedForXFieldtypes1(
            Api.FieldType fieldType,
            string analyzer,
            Interface.IFactoryCollection factory)
        {
            Api.IndexFieldProperties indexFieldProperties = null;
            "Given an index field validator and index field properties".Given(
                () =>
                {
                    indexFieldProperties = new Api.IndexFieldProperties();
                    indexFieldProperties.FieldType = fieldType;
                });

            string.Format(
                "when a field of type '{0}' is validated with 'IndexAnalyzer' & SearchAnalyzer value of '{1}' which is correct",
                fieldType,
                analyzer).When(() => { });

            "then there should be no validation error for 'IndexAnalyzer'".Then(
                () =>
                {
                    indexFieldProperties.IndexAnalyzer = analyzer;
                    Assert.DoesNotThrow(
                        () =>
                            Validator.IndexFieldValidator(
                                factory,
                                new Api.AnalyzerDictionary(),
                                new Api.ScriptDictionary(),
                                "",
                                indexFieldProperties));
                });

            "then there should be no validation error for 'SearchAnalyzer'".Then(
                () =>
                {
                    indexFieldProperties.SearchAnalyzer = analyzer;
                    Assert.DoesNotThrow(
                        () =>
                            Validator.IndexFieldValidator(
                                factory,
                                new Api.AnalyzerDictionary(),
                                new Api.ScriptDictionary(),
                                "",
                                indexFieldProperties));
                });
        }

        [Specification]
        public void DefaultValueTest()
        {
            Api.IndexFieldProperties sut = null;
            "Given a new index field properties".Given(() => sut = new Api.IndexFieldProperties());

            "'standardanalyzer' should be the default 'SearchAnalyzer'".Then(
                () => sut.SearchAnalyzer.Should().Be("standardanalyzer"));
            "'standardanalyzer' should be the default 'IndexAnalyzer'".Then(
                () => sut.IndexAnalyzer.Should().Be("standardanalyzer"));
            "'analyze' should be true".Then(() => sut.Analyze.Should().BeTrue());
            "'store' should be true".Then(() => sut.Store.Should().BeTrue());
            "'index' should be true".Then(() => sut.Index.Should().BeTrue());
            "'FieldType' should be 'Text'".Then(() => sut.FieldType.Should().Be(Api.FieldType.Text));
            "'FieldTermVector' should be 'StoreTermVectorsWithPositionsandOffsets'".Then(
                () => sut.FieldTermVector.Should().Be(Api.FieldTermVector.StoreTermVectorsWithPositionsandOffsets));
        }

        [Thesis]
        [UnitAutoFixture]
        public void StorePropertyCanbeSetToFalse(Interface.IFactoryCollection factory)
        {
            Api.IndexFieldProperties indexFieldProperties = null;
            "Given an index field validator and index field properties".Given(
                () =>
                {
                    indexFieldProperties = new Api.IndexFieldProperties();
                    indexFieldProperties.FieldType = Api.FieldType.Date;
                    indexFieldProperties.Store = false;
                });

            "when a field is validated".When(() => { });

            "then there should be no validation error for 'Store'".Then(
                () =>
                    Assert.DoesNotThrow(
                        () =>
                            Validator.IndexFieldValidator(
                                factory,
                                new Api.AnalyzerDictionary(),
                                new Api.ScriptDictionary(),
                                "",
                                indexFieldProperties)));
        }

        #endregion
    }
}