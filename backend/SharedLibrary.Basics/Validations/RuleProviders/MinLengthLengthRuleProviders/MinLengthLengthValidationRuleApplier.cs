using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.MinLengthLengthRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure a string does not exceed a maximum length.
/// </summary>
/// <summary>
/// Applies FluentValidation rules to ensure a string does not exceed a maximum length.
/// </summary>
public class MinLengthLengthValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;
    private readonly int _minLength;

    public MinLengthLengthValidationRuleApplier(PropertyInfo property)
    {
        _property = property;

        _minLength = int.MaxValue;
        var attribute = property.GetCustomAttribute<MinLengthAttribute>();
        if (attribute != null) _minLength = attribute.Value;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        if (property.PropertyType != typeof(string))
            return;

        var minLength = _minLength;

        validator.RuleFor(x => (string?)_property.GetValue(x))
            .MinimumLength(minLength)
            .WithMessage($"{_property.Name} must be minimum {minLength} characters.")
            .When(x => _property.GetValue(x) is string value && !string.IsNullOrWhiteSpace(value));
    }
}