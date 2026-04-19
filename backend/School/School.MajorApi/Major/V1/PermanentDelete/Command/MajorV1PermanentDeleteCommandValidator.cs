namespace School.MajorApi.Major.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<MajorV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<MajorV1PermanentDeleteCommand>(validationRuleFactory);
