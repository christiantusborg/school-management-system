namespace School.PathwayApi.Pathway.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1CreateCommandValidator(
    ValidationRuleFactory<PathwayV1CreateCommand> validationRuleFactory)
    : BaseValidator<PathwayV1CreateCommand>(validationRuleFactory);
