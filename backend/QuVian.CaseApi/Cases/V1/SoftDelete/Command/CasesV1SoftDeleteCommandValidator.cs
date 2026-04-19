namespace QuVian.CaseApi.Cases.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1SoftDeleteCommandValidator(
    ValidationRuleFactory<CasesV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<CasesV1SoftDeleteCommand>(validationRuleFactory);
