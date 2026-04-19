namespace School.MajorApi.Major.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1CreateCommandValidator(
    ValidationRuleFactory<MajorV1CreateCommand> validationRuleFactory)
    : BaseValidator<MajorV1CreateCommand>(validationRuleFactory);
