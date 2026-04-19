using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.EmailAddressRuleProviders;

/// <summary>
/// Indicates that the string property must be a valid email address.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class EmailAddressAttribute : CommandValidationAttribute
{
    // Marker attribute; no extra implementation required.
}