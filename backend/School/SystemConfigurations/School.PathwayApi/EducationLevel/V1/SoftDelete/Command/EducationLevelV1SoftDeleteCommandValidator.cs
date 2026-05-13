namespace School.PathwayApi.EducationLevel.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EducationLevelV1SoftDeleteCommandValidator(
    ValidationRuleFactory<EducationLevelV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<EducationLevelV1SoftDeleteCommand>(validationRuleFactory);
