namespace School.MajorApi.Major.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1ListCommandValidator(
    ValidationRuleFactory<MajorV1ListCommand> validationRuleFactory)
    : BaseValidator<MajorV1ListCommand>(validationRuleFactory);
