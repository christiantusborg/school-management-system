namespace School.SubjectApi.Subject.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<SubjectV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<SubjectV1PermanentDeleteCommand>(validationRuleFactory);
