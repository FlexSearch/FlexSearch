namespace FlexSearch.Server
{
    using System;
    using System.Collections.Specialized;

    using Common.Logging;
    using Common.Logging.NLog;

    using FlexSearch.Core;

    using Topshelf;

    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            var properties = new NameValueCollection();
            properties["showDateTime"] = "true";
            LogManager.Adapter = new NLogLoggerFactoryAdapter(properties);
            ILog logger = LogManager.GetCurrentClassLogger();
            logger.Info("Loading core services");
           FlexSearch.Core.Main.loadNode();
            //try
            //{
            //    var serverSettings = new Settings.SettingsStore(Constants.ConfFolder.Value + "Config.xml");

            //    HostFactory.Run(
            //        x =>
            //        {
            //            x.Service<FlexServer>(
            //                s =>
            //                {
            //                    s.ConstructUsing(name => new FlexServer());
            //                    s.WhenStarted(tc => tc.Start());
            //                    s.WhenStopped(tc => tc.Stop());
            //                });
            //            x.RunAsLocalSystem();
            //            x.SetDescription("FlexSearch Server");
            //            x.SetDisplayName("FlexSearch Server");
            //            x.SetServiceName("FlexSearchServer");
            //            x.EnableServiceRecovery(rc => rc.RestartService(1));
            //        });
            //}
            //catch (Exception e)
            //{
            //    logger.Fatal(e.Message, e);
            //}
        }

        #endregion
    }
}