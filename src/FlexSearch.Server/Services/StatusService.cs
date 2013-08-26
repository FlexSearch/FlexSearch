namespace FlexSearch.Server.Services
{
    using FlexSearch.Api.Job;

    using ServiceStack.ServiceInterface;

    public class StatusService : Service
    {
        #region Public Methods and Operators

        public GetStatusResponse Any(GetStatus request)
        {
            return this.Cache.Get<GetStatusResponse>(request.Id.ToString());
        }

        #endregion
    }
}