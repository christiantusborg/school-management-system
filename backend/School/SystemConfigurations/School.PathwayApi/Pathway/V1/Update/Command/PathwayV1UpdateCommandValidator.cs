namespace School.PathwayApi.Pathway.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1UpdateCommandValidator(
    ValidationRuleFactory<PathwayV1UpdateCommand> validationRuleFactory)
    : BaseValidator<PathwayV1UpdateCommand>(validationRuleFactory);
