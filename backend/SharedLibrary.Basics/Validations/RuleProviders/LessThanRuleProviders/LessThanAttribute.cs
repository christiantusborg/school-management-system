using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.LessThanRuleProviders;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LessThanAttribute(double value) : CommandValidationAttribute
{
    public readonly double Value = value;
}