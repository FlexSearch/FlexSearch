namespace FlexSearch.Api.Job
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public enum StatusType
    {
        [EnumMember]
        Preparing,

        [EnumMember]
        Started,

        [EnumMember]
        InProgress,

        [EnumMember]
        Error,

        [EnumMember]
        FinshedWithSuccess,

        [EnumMember]
        FinishedWithErrors,

        [EnumMember]
        TerminatedOnRequest
    }
}