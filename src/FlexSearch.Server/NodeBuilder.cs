namespace FlexSearch.Server
{
    using Common.Logging;

    using FlexSearch.Core;

    using Microsoft.FSharp.Core;

    internal class NodeBuilder : Interface.INodeBuilder
    {
        #region Static Fields

        private static ILog logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Public Methods and Operators

        public FSharpOption<Interface.IServer> BuildHttpEndpoint(Interface.IServerSettings obj0)
        {
        }

        public FSharpOption<Interface.IServer> BuildTcpEndpoint(Interface.IServerSettings obj0)
        {
        }

        #endregion
    }
}