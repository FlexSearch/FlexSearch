

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Api.Models
{
    public partial class CsvIndexingRequest : IExtraValidation
    {
        public bool ExtraValidation()
        {
            if (!HasHeaderRecord && (Headers == null || Headers.Length == 0))
            {
                ErrorDescription = "The 'Headers' array must be populated if the file was indicated not to have any headers.";
                ErrorField = "Headers";
                return false;
            }

            return true;
        }
    }
}
