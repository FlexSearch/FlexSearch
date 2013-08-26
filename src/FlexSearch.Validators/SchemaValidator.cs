using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Validators
{
    using FlexSearch.Api.Types;

    using ServiceStack.FluentValidation;
    using ServiceStack.FluentValidation.Results;
    using ServiceStack.FluentValidation.Validators;

    public class SchemaValidator: AbstractValidator<Schema>
    {
        public SchemaValidator()
        {
            this.RuleFor(x => x.SchemaName).SetValidator(new PropertyNameValidator());
        }
    }

 }
