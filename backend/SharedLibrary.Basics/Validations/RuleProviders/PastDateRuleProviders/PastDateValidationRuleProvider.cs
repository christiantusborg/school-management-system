using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.PastDateRuleProviders;

/// <summary>
/// Provider that detects properties marked with [PastDateAttribute].
/// </summary>
public class PastDateValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    /// <summary>
    /// Checks if the property is decorated with PastDateAttribute and is of type DateTime.
    /// </summary>
    public bool CanHandle(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(PastDateAttribute))
               && property.PropertyType == typeof(DateTime);
    }

    /// <summary>
    /// Creates the corresponding rule applier for past date validation.
    /// </summary>
    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new PastDateValidationRuleApplier<T>(property);
    }
}