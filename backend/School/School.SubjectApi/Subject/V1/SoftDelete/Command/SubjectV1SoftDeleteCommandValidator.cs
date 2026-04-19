namespace School.SubjectApi.Subject.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1SoftDeleteCommandValidator(
    ValidationRuleFactory<SubjectV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<SubjectV1SoftDeleteCommand>(validationRuleFactory);
