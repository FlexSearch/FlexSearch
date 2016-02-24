

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Api.Model
{
    public partial class Index : IExtraValidation
    {
        public bool ExtraValidation()
        {
            for (var i = 0; i < PredefinedQueries.Length; i++)
            {
                if (string.IsNullOrEmpty(PredefinedQueries[i].QueryName))
                {
                    ErrorDescription = $"The name of predefined query number {i + 1} doesn't have a name. The query is for index {IndexName} and has the query string {PredefinedQueries[i].QueryString}.";
                    ErrorField = nameof(PredefinedQueries);
                    return false;
                }
            }

            return true;
        }
    }
}
