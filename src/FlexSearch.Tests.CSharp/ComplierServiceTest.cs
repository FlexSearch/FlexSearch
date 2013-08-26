namespace FlexSearch.Tests.CSharp
{
    using System.Collections;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using NUnit.Framework;

    public class ComplierServiceTest
    {
        private Interface.IScriptFactoryCollection scriptFactoryCollection;

        [TestFixtureSetUp]
        public void Init()
        {
            this.scriptFactoryCollection = new CompilerService.ScriptFactoryCollection();
        }

        public class TestFactory
        {
            public static IEnumerable SingleLineScipts
            {
                get
                {
                    yield return new TestCaseData(@"""test""").SetName("Script with constant return value");
                    yield return new TestCaseData("1.ToString()").SetName("Script with simple field concatenation");
                    yield return new TestCaseData("fields[\"anyfield1\"] + fields[\"anyfield1\"]").SetName("Script with field concatenation using field lookup");
                    yield return new TestCaseData("fields[\"anyfield1\"].ToUpper()").SetName("Script with simple text manipulation");               
                }
            }
        }

        [Test, TestCaseSource(typeof(TestFactory), "SingleLineScipts")]
        public void SingleLine_computedfield_script_should_compile(string source)
        {
            var script = new ScriptProperties { ScriptOption = ScriptOption.SingleLine, ScriptType = ScriptType.ComputedField, ScriptSource = source };
            Assert.IsInstanceOf<Interface.IComputedFieldScript>(this.scriptFactoryCollection.ComputedFieldScriptFactory.CompileScript(script));
        }

        [Test, TestCaseSource(typeof(TestFactory), "SingleLineScipts")]
        public void SingleLine_searchprofileselector_script_should_compile(string source)
        {
            var script = new ScriptProperties { ScriptOption = ScriptOption.SingleLine, ScriptType = ScriptType.SearchProfileSelector, ScriptSource = source };
            Assert.IsInstanceOf<Interface.IFlexProfileSelectorScript>(this.scriptFactoryCollection.ProfileSelectorScriptFactory.CompileScript(script));
        }
    }
}
