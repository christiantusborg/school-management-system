namespace School.MajorApi.Major.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1SoftDeleteCommandValidator(
    ValidationRuleFactory<MajorV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<MajorV1SoftDeleteCommand>(validationRuleFactory);
