namespace School.ProgrammeApi.Programme.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1UpdateCommandValidator(
    ValidationRuleFactory<ProgrammeV1UpdateCommand> validationRuleFactory)
    : BaseValidator<ProgrammeV1UpdateCommand>(validationRuleFactory);
