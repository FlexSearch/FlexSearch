

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Api
{
    public interface IDataTransferObject
    {
        /// <summary>
        /// Represents if the object is validated or not.
        /// </summary>
        bool Validated { get; }
        /// <summary>
        /// Description of the error raised by the field
        /// </summary>
        string ErrorDescription { get; }
        /// <summary>
        /// Name of the field in error
        /// </summary>
        string ErrorField { get; }
        /// <summary>
        /// Validate the object
        /// </summary>
        /// <returns>Returns the result of the validation</returns>
        bool Validate();
    }
}
