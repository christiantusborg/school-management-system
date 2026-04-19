namespace School.PathwayApi.Pathway.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1GetCommandValidator(
    ValidationRuleFactory<PathwayV1GetCommand> validationRuleFactory)
    : BaseValidator<PathwayV1GetCommand>(validationRuleFactory);
