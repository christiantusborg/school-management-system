namespace School.ModeOfStudyApi.ModeOfStudy.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1GetCommandValidator(
    ValidationRuleFactory<ModeOfStudyV1GetCommand> validationRuleFactory)
    : BaseValidator<ModeOfStudyV1GetCommand>(validationRuleFactory);
