using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.EmailAddressRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure the property is a valid email address.
/// </summary>
public class EmailAddressValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;

    public EmailAddressValidationRuleApplier(PropertyInfo property)
    {
        _property = property;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        validator.RuleFor(x => (string)_property.GetValue(x)!)
            // Uses FluentValidation's built-in email validator
            .EmailAddress()
            .WithMessage($"{_property.Name} must be a valid email address.")
            // Applies validation only if the value is provided (not null or whitespace)
            .When(x => !string.IsNullOrWhiteSpace((string)_property.GetValue(x)!));
    }
}