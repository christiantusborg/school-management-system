using Ganss.Xss;
using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.RuleProviders.AllowJavaScriptRuleProviders;

namespace QuVian.SharedLibrary.Basics.Validations.RuleProviders.AllowJavaScriptRuleProviders;

/// <summary>
/// Validates that JavaScript is only allowed if AllowJavaScriptAttribute is explicitly applied.
/// </summary>
public class AllowJavaScriptValidationRuleApplier<T> : IValidationRuleApplier<T>
{
    private readonly PropertyInfo _property;
    private readonly HtmlSanitizer _sanitizer = new();

    public AllowJavaScriptValidationRuleApplier(PropertyInfo property)
    {
        _property = property;
    }

    public void Apply(AbstractValidator<T> validator, PropertyInfo property)
    {
        if (property.PropertyType != typeof(string))
            return;

        validator.RuleFor(x => (string?)_property.GetValue(x))
            .Must((obj, value) =>
            {
                if (string.IsNullOrWhiteSpace(value)) return true;

                var containsJs = _sanitizer.Sanitize(value) != value;
                var hasAttribute = _property.GetCustomAttribute<AllowJavaScriptAttribute>() != null;

                // Only valid if JS is present AND AllowJavaScriptAttribute is present
                return !containsJs || hasAttribute;
            })
            .WithMessage($"{_property.Name} contains JavaScript and must be explicitly allowed using [AllowJavaScript].");
    }
}