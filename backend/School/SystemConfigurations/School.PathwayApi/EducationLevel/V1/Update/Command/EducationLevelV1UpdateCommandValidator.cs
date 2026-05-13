namespace School.PathwayApi.EducationLevel.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EducationLevelV1UpdateCommandValidator(
    ValidationRuleFactory<EducationLevelV1UpdateCommand> validationRuleFactory)
    : BaseValidator<EducationLevelV1UpdateCommand>(validationRuleFactory);
