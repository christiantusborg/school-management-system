using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.AnyDateRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure a valid non-default DateTime value.
/// </summary>
public class AnyDateValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;

    public AnyDateValidationRuleApplier(PropertyInfo property)
    {
        _property = property;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        validator.RuleFor(x => (DateTime)_property.GetValue(x)!)
            .Must(date => date != default)
            .WithMessage($"{_property.Name} must be a valid date.")
            .When(x => _property.GetValue(x) is DateTime value && value != default);
    }
}