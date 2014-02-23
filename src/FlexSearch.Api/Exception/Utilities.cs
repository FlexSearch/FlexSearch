using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace FlexSearch.Api.Exception
{
    public partial class InvalidOperation : TBase
    {
        public static InvalidOperation WithDeveloperMessage(InvalidOperation op, string developerMessage) {
            return new InvalidOperation { 
                DeveloperMessage = developerMessage,
                UserMessage = op.UserMessage,
                ErrorCode = op.ErrorCode
            };
        }
    }
}
