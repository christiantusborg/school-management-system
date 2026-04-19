using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.EmailAddressRuleProviders;

/// <summary>
/// Provider that detects properties marked with [EmailAddressAttribute].
/// </summary>
public class EmailAddressValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    /// <summary>
    /// Determines if the property is decorated with EmailAddressAttribute and is of type string.
    /// </summary>
    public bool CanHandle(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(EmailAddressAttribute))
               && property.PropertyType == typeof(string);
    }

    /// <summary>
    /// Creates an instance of the corresponding rule applier for email validation.
    /// </summary>
    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new EmailAddressValidationRuleApplier<T>(property);
    }
}