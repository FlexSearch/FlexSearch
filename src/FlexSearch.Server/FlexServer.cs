namespace FlexSearch.Server
{
    using System;

    using Common.Logging;

    using FlexSearch.Core;

    using Microsoft.Owin.Hosting;

    using Owin;

    public class FlexServer
    {
        #region Fields

        private readonly BootStrapper appHost;

        private readonly ILog logger = LogManager.GetLogger("Init");

        private readonly Interface.IServerSettings serverSettings;

        private IDisposable server;

        #endregion

        #region Constructors and Destructors

        public FlexServer()
        {
            try
            {
                this.serverSettings = new Settings.ServerSettings(Constants.ConfFolder.Value + "Config.xml");
            }
            catch (Exception e)
            {
                this.logger.Fatal("Loading core services: " + "Loading of config.xml failed.", e);
                throw;
            }

            this.logger.Info("Loading core services: config.xml loaded successfully.");
            this.logger.Info("Loading core services: WebServer initialization successful.");
        }

        #endregion

        #region Public Methods and Operators

        public void Start()
        {
            this.server = WebApp.Start<OwinConfiguration>(string.Format("http://*:{0}/", 9800));
        }

        public void Stop()
        {
            //this.server.Dispose();
            this.appHost.StopServer1();
        }

        #endregion
    }

    public class OwinConfiguration
    {
        #region Public Methods and Operators

        public void Configuration(IAppBuilder app)
        {
            app.UseNancy();
        }

        #endregion
    }
}