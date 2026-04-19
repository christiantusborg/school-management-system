namespace QuVian.CaseApi.Cases.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1ListCommandValidator(
    ValidationRuleFactory<CasesV1ListCommand> validationRuleFactory)
    : BaseValidator<CasesV1ListCommand>(validationRuleFactory);
