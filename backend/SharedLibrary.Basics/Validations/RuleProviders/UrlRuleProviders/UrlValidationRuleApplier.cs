using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.UrlRuleProviders;

/// <summary>
/// Applies FluentValidation rules to ensure the property is a valid absolute URL.
/// </summary>
public class UrlValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;

    public UrlValidationRuleApplier(PropertyInfo property)
    {
        _property = property;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        validator.RuleFor(x => (string)_property.GetValue(x)!)
            // Ensures the URL is well-formed and absolute
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                         && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            .WithMessage($"{_property.Name} must be a valid absolute URL.")
            // Only apply validation if value is not null or empty
            .When(x => !string.IsNullOrWhiteSpace((string)_property.GetValue(x)!));
    }
}