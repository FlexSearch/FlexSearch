using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Logging
{
    using FlexSearch.Api;

    using Microsoft.Diagnostics.Tracing;

    public class Logger
    {
        public static Logger Log = new Logger();

        public void AddIndex(Index index)
        {
            FlexLogger.Logger.AddIndex(index.IndexName, index.ToString());
        }
    }

    internal sealed class FlexLogger: EventSource
    {
        public static FlexLogger Logger = new FlexLogger();

        [Event(1, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "")]
        public void AddIndex(string indexName, string indexContent)
        {
            this.WriteEvent(1, indexName, indexContent);
        }
    }
}
