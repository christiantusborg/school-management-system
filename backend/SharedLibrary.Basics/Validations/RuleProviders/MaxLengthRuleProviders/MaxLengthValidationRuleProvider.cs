using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.MaxLengthRuleProviders;

/// <summary>
/// Provider that detects string properties marked with [MaxLengthAttribute].
/// </summary>
public class MaxLengthValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    public bool CanHandle(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(MaxLengthAttribute)) &&
               property.PropertyType == typeof(string);
    }

    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new MaxLengthValidationRuleApplier<T>(property);
    }
}