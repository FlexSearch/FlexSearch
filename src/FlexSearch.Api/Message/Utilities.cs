using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace FlexSearch.Api.Message
{
    public partial class OperationMessage : TBase
    {
        public static OperationMessage WithDeveloperMessage(OperationMessage op, string developerMessage)
        {
            return new OperationMessage
            {
                DeveloperMessage = developerMessage,
                UserMessage = op.UserMessage,
                ErrorCode = op.ErrorCode
            };
        }

        public static OperationMessage WithPropertyName(OperationMessage op, string propertyName)
        {
            return new OperationMessage
            {
                DeveloperMessage = op.DeveloperMessage.Replace("{propertyName}", propertyName),
                UserMessage = op.UserMessage.Replace("{propertyName}", propertyName),
                ErrorCode = op.ErrorCode
            };
        }

        public static OperationMessage WithPropertyName(OperationMessage op, string propertyName, string value)
        {
            return new OperationMessage
            {
                DeveloperMessage = op.DeveloperMessage.Replace("{propertyName}", propertyName).Replace("{value}", value),
                UserMessage = op.UserMessage.Replace("{propertyName}", propertyName).Replace("{value}", value),
                ErrorCode = op.ErrorCode
            };
        }
    }
}
