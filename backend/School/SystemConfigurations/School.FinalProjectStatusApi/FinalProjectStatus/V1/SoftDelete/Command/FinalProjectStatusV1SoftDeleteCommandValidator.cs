namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1SoftDeleteCommandValidator(
    ValidationRuleFactory<FinalProjectStatusV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<FinalProjectStatusV1SoftDeleteCommand>(validationRuleFactory);
