namespace FlexSearch.Server
{
    using ServiceStack.Logging;
    using ServiceStack.Logging.NLogger;

    using Topshelf;

    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            LogManager.LogFactory = new NLogFactory();
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