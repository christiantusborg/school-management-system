namespace School.SubjectApi.Subject.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1RestoreCommandValidator(
    ValidationRuleFactory<SubjectV1RestoreCommand> validationRuleFactory)
    : BaseValidator<SubjectV1RestoreCommand>(validationRuleFactory);
