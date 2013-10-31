using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Server
{
    using System.ComponentModel.Composition.Hosting;

    using Common.Logging;

    using FlexSearch.Core;
    using FlexSearch.Core.Index;

    using Nancy;
    using Nancy.Bootstrapper;
    using Nancy.TinyIoc;

    public class BootStrapper : DefaultNancyBootstrapper
    {
        private readonly Interface.IServerSettings serverSettings;


        public BootStrapper()
        {
        }

        private Interface.IIndexService indexService;
        public BootStrapper(Interface.IServerSettings serverSettings)
        {
            this.serverSettings = serverSettings;
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            CompositionContainer pluginContainer = Factories.PluginContainer(false).Value;
            Interface.IFactoryCollection factoryCollection = new Factories.FactoryCollection(pluginContainer);
            var searchService = new SearchDsl.SearchService(factoryCollection.SearchQueryFactory.GetAllModules());
            Interface.ISettingsBuilder parser = SettingsBuilder.SettingsBuilder(factoryCollection, new Validator.IndexValidator(factoryCollection));
            Interface.IKeyValueStore keyValueStore = new Store.KeyValueStore();
            container.Register(keyValueStore);
            container.Register(factoryCollection);
            container.Register(this.serverSettings);
            this.indexService = new FlexIndexModule.IndexService(parser, searchService, keyValueStore, true);
            container.Register(indexService);   
        }

        public void StopServer1()
        {
            this.ApplicationContainer.Resolve<Interface.IIndexService>().ShutDown();
        }
    }
}
