using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.MinLengthLengthRuleProviders;

/// <summary>
/// Provider that detects string properties marked with [MaxLengthAttribute].
/// </summary>
public class MinLengthLengthValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    public bool CanHandle(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(MinLengthAttribute)) &&
               property.PropertyType == typeof(string);
    }

    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new MinLengthLengthValidationRuleApplier<T>(property);
    }
}