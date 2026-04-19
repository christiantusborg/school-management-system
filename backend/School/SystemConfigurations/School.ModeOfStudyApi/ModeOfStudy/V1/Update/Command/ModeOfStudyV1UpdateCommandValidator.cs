namespace School.ModeOfStudyApi.ModeOfStudy.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1UpdateCommandValidator(
    ValidationRuleFactory<ModeOfStudyV1UpdateCommand> validationRuleFactory)
    : BaseValidator<ModeOfStudyV1UpdateCommand>(validationRuleFactory);
