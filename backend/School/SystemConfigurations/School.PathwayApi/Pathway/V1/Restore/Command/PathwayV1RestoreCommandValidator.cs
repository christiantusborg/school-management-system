namespace School.PathwayApi.Pathway.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1RestoreCommandValidator(
    ValidationRuleFactory<PathwayV1RestoreCommand> validationRuleFactory)
    : BaseValidator<PathwayV1RestoreCommand>(validationRuleFactory);
