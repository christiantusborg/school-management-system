using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.PastDateRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure the property is a past date.
/// </summary>
public class PastDateValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;

    public PastDateValidationRuleApplier(PropertyInfo property)
    {
        _property = property;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        validator.RuleFor(x => (DateTime)_property.GetValue(x)!)
            // Ensures the date is in the past
            .Must(date => date < DateTime.UtcNow)
            .WithMessage($"{_property.Name} must be a past date.")
            // Only apply validation if the value is not the default DateTime
            .When(x => _property.GetValue(x) is DateTime value && value != default);
    }
}