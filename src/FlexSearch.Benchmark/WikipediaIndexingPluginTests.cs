namespace FlexSearch.Benchmark
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks.Dataflow;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Core.Index;
    using FlexSearch.Validators;

    using ServiceStack.Common;
    using ServiceStack.OrmLite;

    internal class WikipediaIndexingPluginTests
    {
        #region Constructors and Destructors

        public WikipediaIndexingPluginTests(string path)
        {
            CompositionContainer pluginContainer = Factories.PluginContainer(false).Value;
            Interface.IFactoryCollection factoryCollection = new Factories.FactoryCollection(pluginContainer);
            Interface.ISettingsBuilder settingsBuilder = SettingsBuilder.SettingsBuilder(
                factoryCollection,
                new IndexValidator(factoryCollection, new IndexValidationParameters(true)));
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

            indexservice.AddIndex(this.WikipediaIndex());
            this.StartIndexing(indexservice, path);
            Console.ReadLine();
        }

        #endregion

        #region Public Methods and Operators

        public Index WikipediaIndex()
        {
            Index index = new Index();
            index.Online = true;

            // Fields
            index.IndexName = "wikipedia";
            index.Fields.Add("title", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("body", new IndexFieldProperties { FieldType = FieldType.Text, Store = false });

            // Properties
            index.Configuration.CommitTimeSec = 500;
            index.Configuration.RefreshTimeMilliSec = 500000;
            index.Configuration.DirectoryType = DirectoryType.MemoryMapped;
            return index;
        }

        #endregion

        #region Methods

        private async void StartIndexing(Interface.IIndexService indexservice, string path)
        {
            int pageCounter = 0;
            string line;

            var file = new StreamReader(path);
            var source = new List<Dictionary<string, string>>(3000000);

            while ((line = file.ReadLine()) != null)
            {
                pageCounter++;
                var document = new Dictionary<string, string>();
                var id = pageCounter;
                document.Add("title", line.Substring(0, line.IndexOf('|') - 1));
                document.Add("body", line.Substring(line.IndexOf('|') + 1));
                source.Add(document);
                //indexservice.SendCommandToQueue(
                //    "wikipedia",
                //    IndexCommand.NewCreate(id.ToString(CultureInfo.InvariantCulture), document));

                if (pageCounter % 100000 == 0)
                {
                    Console.WriteLine(pageCounter);
                }
            }

            Console.WriteLine("Starting Test");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            pageCounter = 0;
            foreach (var item in source)
            {
                pageCounter++;
                await
                    indexservice.CommandQueue()
                        .SendAsync(
                            new Tuple<string, IndexCommand>(
                                "wikipedia",
                                IndexCommand.NewCreate(pageCounter.ToString(CultureInfo.InvariantCulture), item)));
            }

            stopwatch.Stop();
            FileInfo fileInfo = new FileInfo(path);
            double fileSize = fileInfo.Length / 1073741824.0;
            double time = stopwatch.ElapsedMilliseconds / 3600000.0;
            double indexingSpeed = fileSize / time;

            Console.WriteLine("Total Records indexed: " + pageCounter);
            Console.WriteLine("Total Elapsed time (ms): " + stopwatch.ElapsedMilliseconds);
            Console.WriteLine("Total Data Size (MB): " + fileInfo.Length / (1024.0 * 1024.0));
            Console.WriteLine("Indexing Speed (GB/Hr): " + indexingSpeed);
        }

        #endregion
    }
}