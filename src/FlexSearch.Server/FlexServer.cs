namespace FlexSearch.Server
{
    using System;

    using FlexSearch.Core;

    using ServiceStack.Logging;

    public class FlexServer
    {
        #region Fields

        private readonly ServicestackServer appHost;

        private readonly ILog logger = LogManager.GetLogger("Init");

        private readonly Interface.IServerSettings serverSettings;

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

            this.appHost = new ServicestackServer(this.serverSettings);
            this.appHost.Init();
            this.logger.Info("Loading core services: WebServer initialization successful.");
        }

        #endregion

        #region Public Methods and Operators

        public void Start()
        {
            this.logger.Info("Loading core services: Starting webserver on port: " + this.serverSettings.HttpPort());
            this.appHost.Start(string.Format("http://*:{0}/", this.serverSettings.HttpPort()));
            this.logger.Info("Loading core services: Webserver started on port: " + this.serverSettings.HttpPort());
        }

        public void Stop()
        {
            this.appHost.StopServer();
        }

        #endregion
    }
}