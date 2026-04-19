namespace School.SubjectApi.Subject.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1CreateCommandValidator(
    ValidationRuleFactory<SubjectV1CreateCommand> validationRuleFactory)
    : BaseValidator<SubjectV1CreateCommand>(validationRuleFactory);
