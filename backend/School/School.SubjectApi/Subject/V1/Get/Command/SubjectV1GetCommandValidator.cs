namespace School.SubjectApi.Subject.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1GetCommandValidator(
    ValidationRuleFactory<SubjectV1GetCommand> validationRuleFactory)
    : BaseValidator<SubjectV1GetCommand>(validationRuleFactory);
