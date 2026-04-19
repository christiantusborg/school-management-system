namespace School.SubjectApi.Subject.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1UpdateCommandValidator(
    ValidationRuleFactory<SubjectV1UpdateCommand> validationRuleFactory)
    : BaseValidator<SubjectV1UpdateCommand>(validationRuleFactory);
