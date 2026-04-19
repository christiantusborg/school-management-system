using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.MaxLengthRuleProviders;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class MaxLengthAttribute(int max) : CommandValidationAttribute
{
    public readonly int Value = max;
}