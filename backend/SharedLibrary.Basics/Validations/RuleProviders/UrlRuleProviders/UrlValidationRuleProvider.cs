using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.UrlRuleProviders;

/// <summary>
/// Provider that detects properties marked with [UrlAttribute].
/// </summary>
public class UrlValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    /// <summary>
    /// Checks if the property is decorated with UrlAttribute and is of type string.
    /// </summary>
    public bool CanHandle(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(UrlAttribute))
               && property.PropertyType == typeof(string);
    }

    /// <summary>
    /// Creates the corresponding rule applier for URL validation.
    /// </summary>
    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new UrlValidationRuleApplier<T>(property);
    }
}