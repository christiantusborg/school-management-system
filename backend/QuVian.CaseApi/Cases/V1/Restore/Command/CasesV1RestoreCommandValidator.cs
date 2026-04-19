namespace QuVian.CaseApi.Cases.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1RestoreCommandValidator(
    ValidationRuleFactory<CasesV1RestoreCommand> validationRuleFactory)
    : BaseValidator<CasesV1RestoreCommand>(validationRuleFactory);
