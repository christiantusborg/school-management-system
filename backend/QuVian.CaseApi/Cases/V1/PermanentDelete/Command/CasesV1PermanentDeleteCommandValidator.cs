namespace QuVian.CaseApi.Cases.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<CasesV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<CasesV1PermanentDeleteCommand>(validationRuleFactory);
