namespace FlexSearch.Specs.UnitTests.Domain
{
    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    using Xunit;

    public class IndexFieldSpec
    {
        #region Public Methods and Operators

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
                                new AnalyzerDictionary(),
                                new ScriptDictionary(),
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
                                new AnalyzerDictionary(),
                                new ScriptDictionary(),
                                "",
                                indexFieldProperties));
                });
        }

        [Thesis]
        [UnitInlineAutoFixture(FieldType.Text, "test")]
        [UnitInlineAutoFixture(FieldType.Highlight, "test")]
        [UnitInlineAutoFixture(FieldType.Custom, "test")]
        public void CorrectAnalyzersShouldBeSpecifiedForXFieldtypes(
            FieldType fieldType,
            string analyzer,
            IndexFieldProperties indexFieldProperties,
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
                                 new AnalyzerDictionary(),
                                 new ScriptDictionary(),
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
                                new AnalyzerDictionary(),
                                new ScriptDictionary(),
                                "",
                                indexFieldProperties));
                });
        }

        [Thesis]
        [UnitInlineAutoFixture(FieldType.Text, "standardanalyzer")]
        [UnitInlineAutoFixture(FieldType.Highlight, "standardanalyzer")]
        [UnitInlineAutoFixture(FieldType.Custom, "standardanalyzer")]
        public void CorrectAnalyzersShouldBeSpecifiedForXFieldtypes1(
            FieldType fieldType,
            string analyzer,
            Interface.IFactoryCollection factory)
        {
            IndexFieldProperties indexFieldProperties = null;
            "Given an index field validator and index field properties".Given(
                () =>
                {
                    indexFieldProperties = new IndexFieldProperties();
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
                                new AnalyzerDictionary(),
                                new ScriptDictionary(),
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
                                new AnalyzerDictionary(),
                                new ScriptDictionary(),
                                "",
                                indexFieldProperties));
                });
        }

        [Specification]
        public void DefaultValueTest()
        {
            IndexFieldProperties sut = null;
            "Given a new index field properties".Given(() => sut = new IndexFieldProperties());

            "'standardanalyzer' should be the default 'SearchAnalyzer'".Then(
                () => sut.SearchAnalyzer.Should().Be("standardanalyzer"));
            "'standardanalyzer' should be the default 'IndexAnalyzer'".Then(
                () => sut.IndexAnalyzer.Should().Be("standardanalyzer"));
            "'analyze' should be true".Then(() => sut.Analyze.Should().BeTrue());
            "'store' should be true".Then(() => sut.Store.Should().BeTrue());
            "'index' should be true".Then(() => sut.Index.Should().BeTrue());
            "'FieldType' should be 'Text'".Then(() => sut.FieldType.Should().Be(FieldType.Text));
            "'FieldTermVector' should be 'StoreTermVectorsWithPositionsandOffsets'".Then(
                () => sut.FieldTermVector.Should().Be(FieldTermVector.StoreTermVectorsWithPositionsandOffsets));
        }

        [Thesis]
        [UnitAutoFixture]
        public void StorePropertyCanbeSetToFalse(Interface.IFactoryCollection factory)
        {
            IndexFieldProperties indexFieldProperties = null;
            "Given an index field validator and index field properties".Given(
                () =>
                {
                    indexFieldProperties = new IndexFieldProperties();
                    indexFieldProperties.FieldType = FieldType.Date;
                    indexFieldProperties.Store = false;
                });

            "when a field is validated".When(() => { });

            "then there should be no validation error for 'Store'".Then(
                () =>
                    Assert.DoesNotThrow(
                        () =>
                            Validator.IndexFieldValidator(
                                factory,
                                new AnalyzerDictionary(),
                                new ScriptDictionary(),
                                "",
                                indexFieldProperties)));
        }

        #endregion
    }
}