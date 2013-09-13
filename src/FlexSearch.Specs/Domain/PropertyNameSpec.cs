namespace FlexSearch.Specs.Domain
{
    using FlexSearch.Validators;

    using FluentAssertions;

    using Machine.Specifications;

    using ServiceStack.FluentValidation.Results;

    [Subject("Given PropertyName validator")]
    public class When_property_name_is_upper_case : PropertyNameBase
    {
        static ValidationResult result;
        Because of = () => result = Validator.Validate("TEST");
        It it_should_not_be_valid = () => result.IsValid.Should().BeFalse();
    }

    [Subject(typeof(PropertyNameValidator))]
    public class When_property_name_is_mixed_case : PropertyNameBase
    {
        static ValidationResult result;
        Because of = () => result = Validator.Validate("Test");
        It it_should_not_be_valid = () => result.IsValid.Should().BeFalse();
    }

    [Subject(typeof(PropertyNameValidator))]
    public class When_property_name_is_id : PropertyNameBase
    {
        static ValidationResult result;
        Because of = () => result = Validator.Validate("id");
        It it_should_not_be_valid = () => result.IsValid.Should().BeFalse();
    }

    [Subject(typeof(PropertyNameValidator))]
    public class When_property_name_is_type : PropertyNameBase
    {
        static ValidationResult result;
        Because of = () => result = Validator.Validate("id");
        It it_should_not_be_valid = () => result.IsValid.Should().BeFalse();
    }

    [Subject(typeof(PropertyNameValidator))]
    public class When_property_name_is_lastmodified : PropertyNameBase
    {
        static ValidationResult result;
        Because of = () => result = Validator.Validate("id");
        It it_should_not_be_valid = () => result.IsValid.Should().BeFalse();
    }

    [Subject(typeof(PropertyNameValidator))]
    public class When_property_name_is_lowercase : PropertyNameBase
    {
        static ValidationResult result;
        Because of = () => result = Validator.Validate("test");
        It it_should_be_valid = () => result.IsValid.Should().BeTrue();
    }

    [Subject(typeof(PropertyNameValidator))]
    public class When_property_name_is_numeric : PropertyNameBase
    {
        static ValidationResult result;
        Because of = () => result = Validator.Validate("12345");
        It it_should_be_valid = () => result.IsValid.Should().BeTrue();
    }

    public abstract class PropertyNameBase
    {
        public static PropertyNameValidator Validator;
        Establish context = () => { if (Validator == null) Validator = new PropertyNameValidator("test"); };
    }
}
