namespace School.PathwayApi.EducationLevel.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EducationLevelV1ListCommandValidator(
    ValidationRuleFactory<EducationLevelV1ListCommand> validationRuleFactory)
    : BaseValidator<EducationLevelV1ListCommand>(validationRuleFactory);
