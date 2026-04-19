using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.RegularExpressionRuleProviders;

/// <summary>
/// Provider that detects string properties marked with [RegularExpressionAttribute].
/// </summary>
public class RegularExpressionValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    public bool CanHandle(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(RegularExpressionAttribute)) &&
               property.PropertyType == typeof(string);
    }

    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new RegularExpressionValidationRuleApplier<T>(property);
    }
}