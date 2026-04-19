using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.RegularExpressionRuleProviders;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RegularExpressionAttribute(string value) : CommandValidationAttribute
{
    public readonly string Value = value;
}