

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Api
{
    public static class Helpers
    {
        /// <summary>
        /// Returns the FlexSearch field type associated with a given
        /// .net type
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static Constants.FieldType GetFieldType(string dataType)
        {
            switch (dataType)
            {
                case "string":
                    return Constants.FieldType.Text;
                case "datetime":
                    return Constants.FieldType.DateTime;
                case "int":
                    return Constants.FieldType.Int;
                case "long":
                    return Constants.FieldType.Long;
                case "double":
                    return Constants.FieldType.Double;
                default:
                    return Constants.FieldType.Stored;
            }
        }
    }
}
