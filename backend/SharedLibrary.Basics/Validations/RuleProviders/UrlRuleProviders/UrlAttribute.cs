using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.UrlRuleProviders;

/// <summary>
/// Indicates that the string property must be a valid absolute URL.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class UrlAttribute : CommandValidationAttribute
{
    // Marker attribute; no additional logic required.
}