using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.RegularExpressionRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure a string matches a regular expression.
/// </summary>
public class RegularExpressionValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;
    private readonly string? _pattern;

    public RegularExpressionValidationRuleApplier(PropertyInfo property)
    {
        _property = property;

        var attribute = property.GetCustomAttribute<RegularExpressionAttribute>();
        if (attribute != null && property.PropertyType == typeof(string))
        {
            _pattern = attribute.Value;
        }
        else
        {
            _pattern = null;
        }
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        if (string.IsNullOrWhiteSpace(_pattern) || property.PropertyType != typeof(string))
            return;

        validator.RuleFor(x => (string?)_property.GetValue(x))
            .Matches(_pattern)
            .WithMessage($"{_property.Name} is not in a valid format.")
            .When(x => _property.GetValue(x) is string value && !string.IsNullOrWhiteSpace(value));
    }
}