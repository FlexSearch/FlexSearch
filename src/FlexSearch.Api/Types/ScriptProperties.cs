namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class ScriptProperties
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public ScriptOption ScriptOption { get; set; }

        [DataMember(Order = 2)]
        public string ScriptSource { get; set; }

        [DataMember(Order = 3)]
        public ScriptType ScriptType { get; set; }

        #endregion
    }
}