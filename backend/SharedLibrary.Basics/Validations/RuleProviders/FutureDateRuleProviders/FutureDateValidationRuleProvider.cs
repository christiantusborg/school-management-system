using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.FutureDateRuleProviders;

/// <summary>
/// Provider that detects properties marked with [FutureDateAttribute].
/// </summary>
public class FutureDateValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    /// <summary>
    /// Checks if the property is decorated with FutureDateAttribute and is of type DateTime.
    /// </summary>
    public bool CanHandle(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(FutureDateAttribute))
               && property.PropertyType == typeof(DateTime);
    }

    /// <summary>
    /// Creates the corresponding rule applier for future date validation.
    /// </summary>
    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new FutureDateValidationRuleApplier<T>(property);
    }
}