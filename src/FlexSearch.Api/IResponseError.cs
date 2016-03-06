using FlexSearch.Api.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Api
{
    public interface IResponseError
    {
        OperationMessage Error { get; }
    }
}
