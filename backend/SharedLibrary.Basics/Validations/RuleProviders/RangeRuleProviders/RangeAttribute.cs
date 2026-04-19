using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.RangeRuleProviders;

/// <summary>
/// Indicates that a numeric value must fall within a specified inclusive range (supports decimals).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RangeAttribute(double min, double max) : CommandValidationAttribute
{
    public readonly (decimal min, decimal max) Value = ((decimal)min, (decimal)max);
}