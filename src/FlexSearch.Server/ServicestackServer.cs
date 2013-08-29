namespace FlexSearch.Server
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Core.Index;
    using FlexSearch.Validators;

    using Funq;

    using ServiceStack;
    using ServiceStack.Api.Swagger;
    using ServiceStack.CacheAccess;
    using ServiceStack.CacheAccess.Providers;
    using ServiceStack.Common;
    using ServiceStack.Logging;
    using ServiceStack.OrmLite;
    using ServiceStack.Plugins.MsgPack;
    using ServiceStack.Plugins.ProtoBuf;
    using ServiceStack.ServiceInterface.Admin;
    using ServiceStack.ServiceInterface.Cors;
    using ServiceStack.ServiceInterface.Validation;
    using ServiceStack.WebHost.Endpoints;

    public class ServicestackServer : AppHostHttpListenerLongRunningBase
    {
        #region Fields

        private readonly Interface.IServerSettings serverSettings;

        #endregion

        #region Constructors and Destructors

        public ServicestackServer(Interface.IServerSettings serverSettings)
            : base("FlexSearch", typeof(ServicestackServer).Assembly)
        {
            this.serverSettings = serverSettings;
        }

        #endregion

        #region Public Methods and Operators

        public override void Configure(Container container)
        {
            ILog logger = LogManager.GetLogger("Init");

            // Don't send debug information
            this.SetConfig(new EndpointHostConfig { DebugMode = false, });

            // Add all the required plugins
            this.Plugins.Add(new CorsFeature());
            logger.Info("CORS enabled");

            this.Plugins.Add(new MetadataFeature());
            logger.Info("Metadata enabled");

            this.Plugins.Add(new MsgPackFormat());
            logger.Info("Message pack support enabled");

            this.Plugins.Add(new ProtoBufFormat());
            logger.Info("Protobuffer support enabled");

            this.Plugins.Add(new SwaggerFeature());
            logger.Info("Swagger enabled");

            Tuple<bool, int, bool> requestLoggerProperties = this.serverSettings.LoggerProperties();
            if (requestLoggerProperties.Item1)
            {
                this.Plugins.Add(
                    new RequestLogsFeature(3000) { Capacity = requestLoggerProperties.Item2, RequiredRoles = null });
                logger.Info("Request logger enabled with rolling log size of " + requestLoggerProperties.Item2);
            }

            this.Plugins.Add(new ValidationFeature());
            logger.Info("Validation feature enabled");

            container.DefaultReuse = ReuseScope.Container;
            container.RegisterValidators(typeof(IndexValidator).Assembly);

            var dbFactory = new OrmLiteConnectionFactory(Constants.ConfFolder + "/conf.sqlite", SqliteDialect.Provider);
            dbFactory.OpenDbConnection().Run(db => db.CreateTable<Index>(false));
            container.Register<IDbConnectionFactory>(dbFactory);

            // base.Plugins.Add(new ProtoBufFormat())
            logger.Info("All server featured enabled successfully.");

            // Register memory cache store
            container.Register<ICacheClient>(new MemoryCacheClient());

            // container registerations
            // It is very important to register the dependencies in the below order to
            // satisfy mef based dependencies
            CompositionContainer pluginContainer = Factories.PluginContainer(false).Value;
            Interface.IFactoryCollection factoryCollection = new Factories.FactoryCollection(pluginContainer);
            var searchService = new SearchDsl.SearchService(factoryCollection.SearchQueryFactory.GetAllModules());
            Interface.ISettingsBuilder parser = SettingsBuilder.SettingsBuilder(
                factoryCollection,
                new IndexValidator(factoryCollection, new IndexValidationParameters(true)));

            container.Register(factoryCollection);
            container.Register(this.serverSettings);
            container.Register<Interface.IIndexService>(
                new FlexIndexModule.IndexService(parser, searchService, dbFactory, true));

            // Loading plugins after everything else is successful
            Dictionary<string, IPlugin> plugins = factoryCollection.PluginsFactory.GetAllModules();
            foreach (string pluginName in this.serverSettings.PluginsToLoad())
            {
                IPlugin plugin;
                if (plugins.TryGetValue(pluginName, out plugin))
                {
                    this.LoadPlugin(plugin);
                    logger.Info(string.Format("Plugin {0} successfully initialized.", pluginName));
                }
            }

            logger.Info("All server dependencies successfully initialized.");
        }

        public void StopServer()
        {
            ILog logger = LogManager.GetLogger("Init");
            logger.Info("Received shutdown request");
            var iindexService = this.Container.Resolve<Interface.IIndexService>();
            iindexService.ShutDown();
        }

        #endregion
    }
}