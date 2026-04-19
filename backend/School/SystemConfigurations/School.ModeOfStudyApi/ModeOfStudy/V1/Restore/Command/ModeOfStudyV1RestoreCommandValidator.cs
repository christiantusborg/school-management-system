namespace School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1RestoreCommandValidator(
    ValidationRuleFactory<ModeOfStudyV1RestoreCommand> validationRuleFactory)
    : BaseValidator<ModeOfStudyV1RestoreCommand>(validationRuleFactory);
