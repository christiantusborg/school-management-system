namespace QuVian.CaseApi.Cases.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1GetCommandValidator(
    ValidationRuleFactory<CasesV1GetCommand> validationRuleFactory)
    : BaseValidator<CasesV1GetCommand>(validationRuleFactory);
