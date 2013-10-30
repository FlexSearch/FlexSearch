namespace FlexSearch.Server
{
    using System.Collections.Specialized;

    using Common.Logging;
    using Common.Logging.NLog;

    using Topshelf;

    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            var properties = new NameValueCollection();
            properties["showDateTime"] = "true";
            LogManager.Adapter = new NLogLoggerFactoryAdapter(properties);
            ILog logger = LogManager.GetLogger("Init");
            logger.Info("Loading core services");

            HostFactory.Run(
                x =>
                {
                    x.Service<FlexServer>(
                        s =>
                        {
                            s.ConstructUsing(name => new FlexServer());
                            s.WhenStarted(tc => tc.Start());
                            s.WhenStopped(tc => tc.Stop());
                        });
                    x.RunAsLocalSystem();
                    x.SetDescription("FlexSearch indexing Server");
                    x.SetDisplayName("FlexSearch Server");
                    x.SetServiceName("FlexSearchServer");
                    x.EnableServiceRecovery(rc => rc.RestartService(1));
                });
        }

        #endregion
    }
}