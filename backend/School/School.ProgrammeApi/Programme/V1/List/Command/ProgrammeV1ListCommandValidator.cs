namespace School.ProgrammeApi.Programme.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1ListCommandValidator(
    ValidationRuleFactory<ProgrammeV1ListCommand> validationRuleFactory)
    : BaseValidator<ProgrammeV1ListCommand>(validationRuleFactory);
