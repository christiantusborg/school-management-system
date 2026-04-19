namespace School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1SoftDeleteCommandValidator(
    ValidationRuleFactory<ModeOfStudyV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<ModeOfStudyV1SoftDeleteCommand>(validationRuleFactory);
