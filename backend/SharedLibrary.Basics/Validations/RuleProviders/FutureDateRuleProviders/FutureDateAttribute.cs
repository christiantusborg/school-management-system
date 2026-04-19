using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.FutureDateRuleProviders;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class FutureDateAttribute() : CommandValidationAttribute;