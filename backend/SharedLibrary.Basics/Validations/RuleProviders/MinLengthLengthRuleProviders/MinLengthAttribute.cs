using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.MinLengthLengthRuleProviders;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class MinLengthAttribute(int min) : CommandValidationAttribute
{
    public readonly int Value = min;
}