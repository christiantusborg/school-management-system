using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.AnyDateRuleProviders;

/// <summary>
/// Provider that detects properties marked with [AnyDateAttribute].
/// </summary>
public class AnyDateValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    public bool CanHandle(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(AnyDateAttribute))
               && property.PropertyType == typeof(DateTime);
    }

    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new AnyDateValidationRuleApplier<T>(property);
    }
}