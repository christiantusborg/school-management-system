namespace School.ProgrammeApi.Programme.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1CreateCommandValidator(
    ValidationRuleFactory<ProgrammeV1CreateCommand> validationRuleFactory)
    : BaseValidator<ProgrammeV1CreateCommand>(validationRuleFactory);
