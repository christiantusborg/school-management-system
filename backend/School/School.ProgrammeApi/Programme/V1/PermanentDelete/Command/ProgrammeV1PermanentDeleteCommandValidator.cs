namespace School.ProgrammeApi.Programme.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<ProgrammeV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<ProgrammeV1PermanentDeleteCommand>(validationRuleFactory);
