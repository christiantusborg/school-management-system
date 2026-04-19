namespace QuVian.CaseApi.Cases.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1CreateCommandValidator(
    ValidationRuleFactory<CasesV1CreateCommand> validationRuleFactory)
    : BaseValidator<CasesV1CreateCommand>(validationRuleFactory);
