namespace FlexSearch.Specs.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using System.Linq;
    using System.Threading;

    using FlexSearch.Analysis;
    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Core.Index;

    using Microsoft.FSharp.Core;

    using Moq;

    using org.apache.lucene.analysis;

    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;

    using ServiceStack.Common;
    using ServiceStack.OrmLite;

    using Document = FlexSearch.Api.Types.Document;

    internal class MockHelpers
    {
        #region Static Fields

        public static IFixture IntegrationFixture;

        [ThreadStatic]
        public static IFixture UnitFixture;

        #endregion

        #region Public Methods and Operators

        public static void AddTestDataToIndex(Interface.IIndexService indexService, Index index, string testData)
        {
            string[] lines = testData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] headers = lines[0].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines.Skip(1))
            {
                string[] items = line.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                var indexDocument = new Document();
                indexDocument.Id = items[0];
                indexDocument.Index = index.IndexName;
                indexDocument.Fields = new KeyValuePairs();

                for (int i = 1; i < items.Length; i++)
                {
                    indexDocument.Fields.Add(headers[i], items[i]);
                }

                indexService.PerformCommand(
                    index.IndexName,
                    IndexCommand.NewCreate(indexDocument.Id, indexDocument.Fields));
            }

            indexService.PerformCommand(index.IndexName, IndexCommand.Commit);
            Thread.Sleep(100);
        }

        public static Index GetBasicIndexSettingsForContact()
        {
            var index = new Index();
            index.IndexName = "contact";
            index.Online = true;
            index.Configuration.DirectoryType = DirectoryType.Ram;
            index.Fields = new FieldDictionary();

            index.Fields.Add("gender", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("title", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("givenname", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("middleinitial", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("surname", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("streetaddress", new IndexFieldProperties { FieldType = FieldType.Text });

            index.Fields.Add("city", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("state", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("zipcode", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("country", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("countryfull", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("emailaddress", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("username", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("password", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("cctype", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("ccnumber", new IndexFieldProperties { FieldType = FieldType.ExactText });

            index.Fields.Add("occupation", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("cvv2", new IndexFieldProperties { FieldType = FieldType.Int });
            index.Fields.Add("nationalid", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("ups", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("company", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("pounds", new IndexFieldProperties { FieldType = FieldType.Double });
            index.Fields.Add("centimeters", new IndexFieldProperties { FieldType = FieldType.Int });
            index.Fields.Add("guid", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("latitude", new IndexFieldProperties { FieldType = FieldType.Double });
            index.Fields.Add("longitude", new IndexFieldProperties { FieldType = FieldType.Double });

            index.Fields.Add("importdate", new IndexFieldProperties { FieldType = FieldType.Date });
            index.Fields.Add("timestamp", new IndexFieldProperties { FieldType = FieldType.DateTime });
            index.Fields.Add("topic", new IndexFieldProperties{FieldType = FieldType.ExactText});
            index.Fields.Add("abstract", new IndexFieldProperties { FieldType = FieldType.Text });
            // Computed fields
            index.Fields.Add(
                "fullname",
                new IndexFieldProperties { FieldType = FieldType.Text, ScriptName = "fullname" });
            index.Scripts.Add(
                "fullname",
                new ScriptProperties
                {
                    ScriptOption = ScriptOption.SingleLine,
                    ScriptType = ScriptType.ComputedField,
                    ScriptSource = "fields[\"givenname\"] + \" \" + fields[\"surname\"]"
                });
            return index;
        }

        public static IFixture IntegartionFixtureSetup()
        {
            if (IntegrationFixture != null)
            {
                return IntegrationFixture;
            }

            IntegrationFixture = new Fixture().Customize(new AutoMoqCustomization());

            CompositionContainer pluginContainer = Factories.PluginContainer(false).Value;
            Interface.IFactoryCollection factoryCollection = new Factories.FactoryCollection(pluginContainer);
            Interface.ISettingsBuilder settingsBuilder = SettingsBuilder.SettingsBuilder(
                factoryCollection,
                new Validator.IndexValidator(factoryCollection));
            var searchService = new SearchDsl.SearchService(factoryCollection.SearchQueryFactory.GetAllModules());
            var dbFactory = new OrmLiteConnectionFactory(
                Constants.ConfFolder.Value + "//conf.sqlite",
                SqliteDialect.Provider);

            dbFactory.OpenDbConnection().Run(db => db.CreateTable<Index>(true));
            Interface.IIndexService indexservice = new FlexIndexModule.IndexService(
                settingsBuilder,
                searchService,
                dbFactory.Open(),
                false);

            IntegrationFixture.Register(GetBasicIndexSettingsForContact);
            IntegrationFixture.Register(() => pluginContainer);
            IntegrationFixture.Register(() => factoryCollection);
            IntegrationFixture.Register(() => settingsBuilder);
            IntegrationFixture.Register(() => searchService);
            IntegrationFixture.Register(() => indexservice);
            return IntegrationFixture;
        }

        public static IFixture UnitFixtureSetup()
        {
            if (UnitFixture != null)
            {
                return UnitFixture;
            }

            UnitFixture = new Fixture().Customize(new AutoMoqCustomization());
            var factoryCollection = new Mock<Interface.IFactoryCollection>();

            // Filter factory setup
            factoryCollection.Setup(x => x.FilterFactory.ModuleExists("test")).Returns(false);
            factoryCollection.Setup(x => x.FilterFactory.ModuleExists("standardfilter")).Returns(true);
            factoryCollection.Setup(x => x.FilterFactory.GetModuleByName("standardfilter"))
                .Returns(FSharpOption<Interface.IFlexFilterFactory>.Some(new Filters.StandardFilterFactory()));

            // Tokenizer factory setup
            factoryCollection.Setup(x => x.TokenizerFactory.ModuleExists("test")).Returns(false);
            factoryCollection.Setup(x => x.TokenizerFactory.ModuleExists("standardtokenizer")).Returns(true);
            factoryCollection.Setup(x => x.TokenizerFactory.GetModuleByName("standardtokenizer"))
                .Returns(FSharpOption<Interface.IFlexTokenizerFactory>.Some(new Tokenizers.StandardTokenizerFactory()));

            // Analyzer factory setup
            factoryCollection.Setup(x => x.AnalyzerFactory.ModuleExists("test")).Returns(false);
            factoryCollection.Setup(x => x.AnalyzerFactory.ModuleExists("standardanalyzer")).Returns(true);
            factoryCollection.Setup(x => x.AnalyzerFactory.GetModuleByName("standardanalyzer"))
                .Returns(FSharpOption<Analyzer>.Some(Analyzers.FlexStandardAnalyzer));

            var resourceLoader = new Mock<Interface.IResourceLoader>();
            resourceLoader.Setup(x => x.LoadResourceAsList("wordlist.txt")).Returns(new List<string> { "hello", "world" });
            resourceLoader.Setup(x => x.LoadResourceAsMap("synonymlist.txt"))
                .Returns(new List<string[]> { { new[] { "easy", "simple", "clear" } } });

            Interface.IScriptFactoryCollection scriptFactory = new CompilerService.ScriptFactoryCollection();
            UnitFixture.Register(() => scriptFactory);
            UnitFixture.Register(() => resourceLoader.Object);
            UnitFixture.Register(() => factoryCollection.Object);
            return UnitFixture;
        }

        #endregion
    }
}