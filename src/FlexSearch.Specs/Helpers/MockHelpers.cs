namespace FlexSearch.Specs.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using System.Linq;
    using System.Threading;

    using FlexSearch.Analysis;
    using FlexSearch.Core;

    using Microsoft.FSharp.Core;

    using Moq;

    using org.apache.lucene.analysis;

    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;


    using Document = FlexSearch.Api.Document;

    internal class MockHelpers
    {
        #region Static Fields

        public static IFixture IntegrationFixture;

        [ThreadStatic]
        public static IFixture UnitFixture;

        #endregion

        #region Public Methods and Operators

        public static void AddTestDataToIndex(Interface.IIndexService indexService, Api.Index index, string testData)
        {
            string[] lines = testData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] headers = lines[0].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines.Skip(1))
            {
                string[] items = line.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                var indexDocument = new Document();
                indexDocument.Id = items[0];
                indexDocument.Index = index.IndexName;
                indexDocument.Fields = new Api.KeyValuePairs();

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

        public static Api.Index GetBasicIndexSettingsForContact()
        {
            var index = new Api.Index();
            index.IndexName = "contact";
            index.Online = true;
            index.Configuration.DirectoryType = Api.DirectoryType.Ram;
            index.Fields = new Api.FieldDictionary();

            index.Fields.Add("gender", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("title", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("givenname", new Api.IndexFieldProperties { FieldType = Api.FieldType.Text });
            index.Fields.Add("middleinitial", new Api.IndexFieldProperties { FieldType = Api.FieldType.Text });
            index.Fields.Add("surname", new Api.IndexFieldProperties { FieldType = Api.FieldType.Text });
            index.Fields.Add("streetaddress", new Api.IndexFieldProperties { FieldType = Api.FieldType.Text });

            index.Fields.Add("city", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("state", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("zipcode", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("country", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("countryfull", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("emailaddress", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("username", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("password", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("cctype", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("ccnumber", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });

            index.Fields.Add("occupation", new Api.IndexFieldProperties { FieldType = Api.FieldType.Text });
            index.Fields.Add("cvv2", new Api.IndexFieldProperties { FieldType = Api.FieldType.Int });
            index.Fields.Add("nationalid", value: new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("ups", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("company", new Api.IndexFieldProperties { FieldType = Api.FieldType.Text });
            index.Fields.Add("pounds", new Api.IndexFieldProperties { FieldType = Api.FieldType.Double });
            index.Fields.Add("centimeters", new Api.IndexFieldProperties { FieldType = Api.FieldType.Int });
            index.Fields.Add("guid", new Api.IndexFieldProperties { FieldType = Api.FieldType.ExactText });
            index.Fields.Add("latitude", new Api.IndexFieldProperties { FieldType = Api.FieldType.Double });
            index.Fields.Add("longitude", new Api.IndexFieldProperties { FieldType = Api.FieldType.Double });

            index.Fields.Add("importdate", new Api.IndexFieldProperties { FieldType = Api.FieldType.Date });
            index.Fields.Add("timestamp", new Api.IndexFieldProperties { FieldType = Api.FieldType.DateTime });
            index.Fields.Add("topic", new Api.IndexFieldProperties{FieldType = Api.FieldType.ExactText});
            index.Fields.Add("abstract", new Api.IndexFieldProperties { FieldType = Api.FieldType.Text });
            // Computed fields
            index.Fields.Add(
                "fullname",
                new Api.IndexFieldProperties { FieldType = Api.FieldType.Text, ScriptName = "fullname" });
            index.Scripts.Add(
                "fullname",
                new Api.ScriptProperties
                {
                    ScriptOption = Api.ScriptOption.SingleLine,
                    ScriptType = Api.ScriptType.ComputedField,
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
            //var dbFactory = new OrmLiteConnectionFactory(
            //    Constants.ConfFolder.Value + "//conf.sqlite",
            //    SqliteDialect.Provider);

            //dbFactory.OpenDbConnection().Run(db => db.CreateTable<Index>(true));
            //Interface.IIndexService indexservice = new FlexIndexModule.IndexService(
            //    settingsBuilder,
            //    searchService,
            //    dbFactory.Open(),
            //    false);

            IntegrationFixture.Register(GetBasicIndexSettingsForContact);
            IntegrationFixture.Register(() => pluginContainer);
            IntegrationFixture.Register(() => factoryCollection);
            IntegrationFixture.Register(() => settingsBuilder);
            IntegrationFixture.Register(() => searchService);
            //IntegrationFixture.Register(() => indexservice);
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