namespace FlexSearch.Api.Job
{
    using System;
    using System.Runtime.Serialization;

    using ServiceStack.ServiceInterface.ServiceModel;

    [DataContract(Namespace = "")]
    public class GetStatusResponse
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public string Description { get; set; }

        [DataMember(Order = 2)]
        public DateTime EndTime { get; set; }

        [DataMember(Order = 3)]
        public string Message { get; set; }

        [DataMember(Order = 4)]
        public ResponseStatus ResponseStatus { get; set; }

        [DataMember(Order = 5)]
        public DateTime StartTime { get; set; }

        [DataMember(Order = 6)]
        public StatusType Status { get; set; }

        #endregion
    }
}