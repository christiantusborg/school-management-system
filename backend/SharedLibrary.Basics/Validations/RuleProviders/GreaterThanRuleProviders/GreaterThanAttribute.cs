using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.GreaterThanRuleProviders;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GreaterThanAttribute(double value) : CommandValidationAttribute
{
    public readonly double Value = value;
}