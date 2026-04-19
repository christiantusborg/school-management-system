using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.FutureDateRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure the property is a future date.
/// </summary>
public class FutureDateValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;

    public FutureDateValidationRuleApplier(PropertyInfo property)
    {
        _property = property;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        validator.RuleFor(x => (DateTime)_property.GetValue(x)!)
            // Ensures the date is in the future
            .Must(date => date > DateTime.UtcNow)
            .WithMessage($"{_property.Name} must be a future date.")
            // Only apply validation if the value is not the default DateTime (optional safeguard)
            .When(x => _property.GetValue(x) is DateTime value && value != default);
    }
}