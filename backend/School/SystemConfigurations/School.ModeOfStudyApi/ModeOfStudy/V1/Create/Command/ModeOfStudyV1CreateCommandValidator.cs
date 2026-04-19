namespace School.ModeOfStudyApi.ModeOfStudy.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1CreateCommandValidator(
    ValidationRuleFactory<ModeOfStudyV1CreateCommand> validationRuleFactory)
    : BaseValidator<ModeOfStudyV1CreateCommand>(validationRuleFactory);
