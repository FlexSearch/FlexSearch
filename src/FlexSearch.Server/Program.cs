namespace FlexSearch.Server
{
    using System;
    using System.Collections.Specialized;
    using FlexSearch.Core;
    using Topshelf;
    using Owin;

    internal class Program
    {
        #region Methods


        private static void Main(string[] args)
        {
            try
            {
                var settings = Core.Main.GetServerSettings(Constants.ConfFolder.Value + "Config.json");

                HostFactory.Run(
                    x =>
                    {
                        x.Service<Main.NodeService>(
                            s =>
                            {
                                s.ConstructUsing(name => new Main.NodeService(settings, false));
                                s.WhenStarted(tc => tc.Start());
                                s.WhenStopped(tc => tc.Stop());
                            });
                        x.RunAsLocalSystem();
                        x.SetDescription("FlexSearch Server");
                        x.SetDisplayName("FlexSearch Server");
                        x.SetServiceName("FlexSearch-Server");
                        x.EnableServiceRecovery(rc => rc.RestartService(1));
                    });
            }
            catch (Exception e)
            {
                //logger.Fatal(e.Message, e);
            }
        }

        #endregion
    }
}