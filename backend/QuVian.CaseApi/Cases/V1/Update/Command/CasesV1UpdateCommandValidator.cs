namespace QuVian.CaseApi.Cases.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1UpdateCommandValidator(
    ValidationRuleFactory<CasesV1UpdateCommand> validationRuleFactory)
    : BaseValidator<CasesV1UpdateCommand>(validationRuleFactory);
