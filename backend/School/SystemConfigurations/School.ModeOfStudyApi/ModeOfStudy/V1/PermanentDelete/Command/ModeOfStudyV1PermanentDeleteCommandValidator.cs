namespace School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<ModeOfStudyV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<ModeOfStudyV1PermanentDeleteCommand>(validationRuleFactory);
