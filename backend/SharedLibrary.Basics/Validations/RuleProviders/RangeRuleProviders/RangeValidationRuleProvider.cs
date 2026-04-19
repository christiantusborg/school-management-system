using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.RangeRuleProviders;

/// <summary>
/// Provider that detects numeric properties marked with [RangeAttribute].
/// </summary>
public class RangeValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    public bool CanHandle(PropertyInfo property)
    {
        var type = property.PropertyType;
        return Attribute.IsDefined(property, typeof(RangeAttribute))
               && (type == typeof(int) || type == typeof(double) || type == typeof(float)
                   || type == typeof(decimal) || type == typeof(long) || type == typeof(short));
    }

    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new RangeValidationRuleApplier<T>(property);
    }
}