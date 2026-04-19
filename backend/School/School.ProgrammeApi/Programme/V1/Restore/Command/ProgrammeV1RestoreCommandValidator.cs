namespace School.ProgrammeApi.Programme.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1RestoreCommandValidator(
    ValidationRuleFactory<ProgrammeV1RestoreCommand> validationRuleFactory)
    : BaseValidator<ProgrammeV1RestoreCommand>(validationRuleFactory);
