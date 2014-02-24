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

        public static InvalidOperation WithPropertyName(InvalidOperation op, string propertyName)
        {
            return new InvalidOperation
            {
                DeveloperMessage = op.DeveloperMessage.Replace("{propertyName}", propertyName),
                UserMessage = op.UserMessage.Replace("{propertyName}", propertyName),
                ErrorCode = op.ErrorCode
            };
        }

        public static InvalidOperation WithPropertyName(InvalidOperation op, string propertyName, string value)
        {
            return new InvalidOperation
            {
                DeveloperMessage = op.DeveloperMessage.Replace("{propertyName}", propertyName).Replace("{value}", value),
                UserMessage = op.UserMessage.Replace("{propertyName}", propertyName).Replace("{value}", value),
                ErrorCode = op.ErrorCode
            };
        }
    }
}
