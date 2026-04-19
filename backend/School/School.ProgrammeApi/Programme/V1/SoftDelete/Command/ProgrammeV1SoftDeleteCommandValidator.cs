namespace School.ProgrammeApi.Programme.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1SoftDeleteCommandValidator(
    ValidationRuleFactory<ProgrammeV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<ProgrammeV1SoftDeleteCommand>(validationRuleFactory);
