namespace School.ModeOfStudyApi.ModeOfStudy.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1ListCommandValidator(
    ValidationRuleFactory<ModeOfStudyV1ListCommand> validationRuleFactory)
    : BaseValidator<ModeOfStudyV1ListCommand>(validationRuleFactory);
