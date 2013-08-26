namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract]
    public enum ScriptOption
    {
        SingleLine,

        MultiLine,

        FileBased
    }
}