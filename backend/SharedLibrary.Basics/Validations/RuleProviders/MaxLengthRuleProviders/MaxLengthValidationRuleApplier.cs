using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.MaxLengthRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure a string does not exceed a maximum length.
/// </summary>
/// <summary>
/// Applies FluentValidation rules to ensure a string does not exceed a maximum length.
/// </summary>
public class MaxLengthValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;
    private readonly int _maxLength;

    public MaxLengthValidationRuleApplier(PropertyInfo property)
    {
        _property = property;

        _maxLength = int.MaxValue;
        var attribute = property.GetCustomAttribute<MaxLengthAttribute>();
        if (attribute != null) _maxLength = attribute.Value;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        if (property.PropertyType != typeof(string))
            return;

        var maxLength = _maxLength;

        validator.RuleFor(x => (string?)_property.GetValue(x))
            .MaximumLength(maxLength)
            .WithMessage($"{_property.Name} must not exceed {maxLength} characters.")
            .When(x => _property.GetValue(x) is string value && !string.IsNullOrWhiteSpace(value));
    }
}