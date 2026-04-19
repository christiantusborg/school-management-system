using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Basics.Validations.RuleProviders.AllowJavaScriptRuleProviders;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.RuleProviders.AllowJavaScriptRuleProviders;

/// <summary>
/// Provider that detects string properties (runs on all) and applies logic based on AllowJavaScriptAttribute.
/// </summary>
public class AllowJavaScriptValidationRuleProvider<T> : IValidationRuleProvider<T>
{
    public bool CanHandle(PropertyInfo property)
    {
        return property.PropertyType == typeof(string);
    }

    public IValidationRuleApplier<T> CreateRule(PropertyInfo property)
    {
        return new AllowJavaScriptValidationRuleApplier<T>(property);
    }
}