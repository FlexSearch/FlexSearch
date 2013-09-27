namespace FlexSearch.Specs.UnitTests
{
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    using Xunit.Extensions;

    public class CompilerServiceSpec
    {
        #region Fields

        private Interface.IScriptFactoryCollection factoryCollection = new CompilerService.ScriptFactoryCollection();

        #endregion

        #region Public Properties

        public static IEnumerable<object[]> SingleLineScriptCases
        {
            get
            {
                return new[]
                       {
                           new object[] { "Script with constant return value must compile", @"""test""" },
                           new object[] { "Script with simple .net method must compile", @"""1.ToString()""" },
                           new object[] { "Script with field lookup must compile", "fields[\"anyfield1\"] + fields[\"anyfield1\"]" },
                           new object[] { "Script with transformation must compile", "fields[\"anyfield1\"].ToUpper()" },
                       };
            }
        }

        public static IEnumerable<object[]> MultiLineScriptCases
        {
            get
            {
                return new[]
                       {
                           new object[] { "Script with constant return value must compile", "return \"test\";" },
                           new object[] { "Script with simple .net method must compile", "return 1.ToString();" },
                           new object[] { "Script with field lookup must compile", "return fields[\"anyfield1\"] + fields[\"anyfield1\"];" },
                           new object[] { "Script with transformation must compile", "return fields[\"anyfield1\"].ToUpper();" },
                       };
            }
        }
        #endregion

        #region Public Methods and Operators

        [Thesis]
        [PropertyData("SingleLineScriptCases")]
        public void SingleLineScriptsTests(string message, string source)
        {
          "Given a scriptFactory".Given(() => { });
            string.Format("Computed field: {0}", message).Observation(
                () =>
                {
                    var script = new ScriptProperties { ScriptOption = ScriptOption.SingleLine, ScriptType = ScriptType.ComputedField, ScriptSource = source };
                    this.factoryCollection.ComputedFieldScriptFactory.CompileScript(script)
                        .Should()
                        .BeAssignableTo<Interface.IComputedFieldScript>();
                });
            string.Format("Profile Selector: {0}", message).Observation(
                () =>
                {
                    var script = new ScriptProperties { ScriptOption = ScriptOption.SingleLine, ScriptType = ScriptType.SearchProfileSelector, ScriptSource = source };
                    this.factoryCollection.ProfileSelectorScriptFactory.CompileScript(script)
                        .Should()
                        .BeAssignableTo<Interface.IFlexProfileSelectorScript>();
                });
            
        }

        [Thesis]
        [PropertyData("MultiLineScriptCases")]
        public void MultiLineScriptsTests(string message, string source)
        {
            "Given a scriptFactory".Given(() => { });
            string.Format("Computed field: {0}", message).Observation(
                () =>
                {
                    var script = new ScriptProperties { ScriptOption = ScriptOption.MultiLine, ScriptType = ScriptType.ComputedField, ScriptSource = source };
                    this.factoryCollection.ComputedFieldScriptFactory.CompileScript(script)
                        .Should()
                        .BeAssignableTo<Interface.IComputedFieldScript>();
                });
            string.Format("Profile Selector: {0}", message).Observation(
                () =>
                {
                    var script = new ScriptProperties { ScriptOption = ScriptOption.MultiLine, ScriptType = ScriptType.SearchProfileSelector, ScriptSource = source };
                    this.factoryCollection.ProfileSelectorScriptFactory.CompileScript(script)
                        .Should()
                        .BeAssignableTo<Interface.IFlexProfileSelectorScript>();
                });

        }
        #endregion
    }
}