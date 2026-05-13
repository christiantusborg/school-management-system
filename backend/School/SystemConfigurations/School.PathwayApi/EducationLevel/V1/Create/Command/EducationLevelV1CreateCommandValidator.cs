namespace School.PathwayApi.EducationLevel.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EducationLevelV1CreateCommandValidator(
    ValidationRuleFactory<EducationLevelV1CreateCommand> validationRuleFactory)
    : BaseValidator<EducationLevelV1CreateCommand>(validationRuleFactory);
