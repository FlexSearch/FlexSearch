namespace FlexSearch.Server
{
    using System;
    using System.Threading;

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

            this.appHost = new BootStrapper(this.serverSettings);
            this.logger.Info("Loading core services: WebServer initialization successful.");
        }

        #endregion

        #region Public Methods and Operators

        public void Start()
        {
            var serverThread = new Thread(
                () =>
                {                   
                    using (WebApp.Start<OwinConfiguration>(string.Format("http://*:{0}/", 9800))) // this.serverSettings.HttpPort())))
                    {
                        Console.WriteLine("Press enter to exit");
                        Console.ReadLine();
                    }
                });

            serverThread.Start();
            //this.logger.Info("Loading core services: Starting webserver on port: " + this.serverSettings.HttpPort());
            // this.logger.Info("Loading core services: Webserver started on port: " + this.serverSettings.HttpPort());
        }

        public void Stop()
        {
            this.appHost.StopServer();
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