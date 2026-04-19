using QuVian.SharedLibrary.Validations.Attributes;

namespace QuVian.SharedLibrary.Validations.RuleProviders.AllowJavaScriptRuleProviders;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class AllowJavaScriptAttribute : CommandValidationAttribute;