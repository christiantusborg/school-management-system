using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.RangeRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure a numeric value falls within a specified integer range.
/// Works for all numeric types (int, float, double, decimal, long, short).
/// </summary>
public class RangeValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;
    private readonly decimal _min;
    private readonly decimal _max;

    public RangeValidationRuleApplier(PropertyInfo property)
    {
        _property = property;
        var attribute = property.GetCustomAttribute<RangeAttribute>();
        if (attribute == null)
            throw new ArgumentException("RangeAttribute must be defined on the property.");

        (_min, _max) = (attribute.Value.min, attribute.Value.max);
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        validator.RuleFor(x => Convert.ToDecimal(_property.GetValue(x)!))
            .InclusiveBetween(_min, _max)
            .WithMessage($"{_property.Name} must be between {_min} and {_max}.")
            .When(x => _property.GetValue(x) != null);
    }
}