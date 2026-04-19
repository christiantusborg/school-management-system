namespace School.ProgrammeApi.Programme.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1GetCommandValidator(
    ValidationRuleFactory<ProgrammeV1GetCommand> validationRuleFactory)
    : BaseValidator<ProgrammeV1GetCommand>(validationRuleFactory);
