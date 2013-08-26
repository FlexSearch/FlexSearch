namespace FlexSearch.Api.Job
{
    using System.Runtime.Serialization;

    [DataContract]
    public enum StatusType
    {
        Preparing,

        Started,

        InProgress,

        Error,

        FinshedWithSuccess,

        FinishedWithErrors,

        TerminatedOnRequest
    }
}