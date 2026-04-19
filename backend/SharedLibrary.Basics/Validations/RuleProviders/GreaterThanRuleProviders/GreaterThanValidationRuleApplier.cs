using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.GreaterThanRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure the numeric value is greater than a specified minimum.
/// </summary>
public class GreaterThanValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;
    private readonly double _minimum;

    public GreaterThanValidationRuleApplier(PropertyInfo property)
    {
        _property = property;
        var attribute = property.GetCustomAttribute<GreaterThanAttribute>();
        if (attribute == null)
            throw new ArgumentException("GreaterThanAttribute must be defined on the property.");

        _minimum = attribute.Value;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        validator.RuleFor(x => Convert.ToDouble(_property.GetValue(x)!))
            .Must(value => value > _minimum)
            .WithMessage($"{_property.Name} must be greater than {_minimum}.")
            .When(x => _property.GetValue(x) != null);
    }
}