namespace School.MajorApi.Major.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1UpdateCommandValidator(
    ValidationRuleFactory<MajorV1UpdateCommand> validationRuleFactory)
    : BaseValidator<MajorV1UpdateCommand>(validationRuleFactory);
