namespace FlexSearch.Logging
{
    using Microsoft.Diagnostics.Tracing;


    [EventSource(Name = "FlexSearch")]
    public sealed partial class FlexLogger : EventSource
    {
        #region Keywords / Tasks / Opcodes

        /// <summary>
        /// By defining keywords, we can turn on events independently.   Because we defined the 'Request'
        /// and 'Debug' keywords and assigned the 'Request' keywords to the first three events, these 
        /// can be turned on and off by setting this bit when you enable the EventSource.   Similarly
        /// the 'Debug' event can be turned on and off independently.  
        /// </summary>
        public class Keywords   // This is a bitvector
        {
            public const EventKeywords IndexOperation = (EventKeywords)0x0001;
            public const EventKeywords Search = (EventKeywords)0x0002;
        }

        public class Tasks
        {
            public const EventTask Index = (EventTask)0x1;
            public const EventTask Search = (EventTask)0x2;
            public const EventTask MefResolver = (EventTask)0x3;
        }

        #endregion

        public static FlexLogger Logger = new FlexLogger();
        [Event(1, Channel = EventChannel.Admin, Level = EventLevel.Informational, Task = Tasks.Index, Keywords = Keywords.IndexOperation, Message = "Adding index '{0}'")]
        public void AddIndex(string indexName, string indexContent)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(1, indexName, indexContent);
            }
        }
    }
}
