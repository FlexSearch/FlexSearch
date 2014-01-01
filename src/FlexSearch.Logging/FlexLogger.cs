namespace FlexSearch.Logging
{
    using Microsoft.Diagnostics.Tracing;

    public sealed partial class FlexLogger : EventSource
    {
        public static FlexLogger Logger = new FlexLogger();

        [Event(1, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "")]
        public void AddIndex(string indexName, string indexContent)
        {
            this.WriteEvent(1, indexName, indexContent);
        }
    }
}
