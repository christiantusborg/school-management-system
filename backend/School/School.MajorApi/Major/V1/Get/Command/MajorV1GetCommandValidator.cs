namespace School.MajorApi.Major.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1GetCommandValidator(
    ValidationRuleFactory<MajorV1GetCommand> validationRuleFactory)
    : BaseValidator<MajorV1GetCommand>(validationRuleFactory);
