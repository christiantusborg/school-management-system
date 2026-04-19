namespace School.MajorApi.Major.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1RestoreCommandValidator(
    ValidationRuleFactory<MajorV1RestoreCommand> validationRuleFactory)
    : BaseValidator<MajorV1RestoreCommand>(validationRuleFactory);
