namespace School.PathwayApi.Pathway.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1SoftDeleteCommandValidator(
    ValidationRuleFactory<PathwayV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<PathwayV1SoftDeleteCommand>(validationRuleFactory);
