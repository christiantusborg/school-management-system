using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.PastDateRuleProviders;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PastDateAttribute() : CommandValidationAttribute;