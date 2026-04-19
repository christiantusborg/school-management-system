namespace School.SubjectApi.Subject.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1ListCommandValidator(
    ValidationRuleFactory<SubjectV1ListCommand> validationRuleFactory)
    : BaseValidator<SubjectV1ListCommand>(validationRuleFactory);
