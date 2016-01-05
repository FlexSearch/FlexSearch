

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlexSearch.Api.Constants;

namespace FlexSearch.Api.Model
{
    public partial class Field
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Field" /> class.
        /// </summary>
        /// <param name="indexName"></param>
        public Field(string fieldName, FieldType fieldType)
        {
            this.FieldName = fieldName;
            this.FieldType = fieldType;
        }
    }
}