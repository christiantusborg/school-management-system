using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.LessThanRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure the numeric value is less than a specified maximum.
/// </summary>
public class LessThanValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;
    private readonly double _maxValue;

    public LessThanValidationRuleApplier(PropertyInfo property)
    {
        _property = property;
        var attribute = property.GetCustomAttribute<LessThanAttribute>();
        if (attribute == null)
            throw new ArgumentException("LessThanAttribute must be defined on the property.");

        _maxValue = attribute.Value;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        validator.RuleFor(x => Convert.ToDouble(_property.GetValue(x)!))
            .Must(value => value < _maxValue)
            .WithMessage($"{_property.Name} must be less than {_maxValue}.")
            .When(x => _property.GetValue(x) != null);
    }
}