namespace School.PathwayApi.Pathway.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1ListCommandValidator(
    ValidationRuleFactory<PathwayV1ListCommand> validationRuleFactory)
    : BaseValidator<PathwayV1ListCommand>(validationRuleFactory);
