namespace School.PathwayApi.Pathway.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<PathwayV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<PathwayV1PermanentDeleteCommand>(validationRuleFactory);
