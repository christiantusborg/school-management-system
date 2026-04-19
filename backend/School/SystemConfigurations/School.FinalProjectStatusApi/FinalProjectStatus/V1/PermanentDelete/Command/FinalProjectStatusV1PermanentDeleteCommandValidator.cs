namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<FinalProjectStatusV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<FinalProjectStatusV1PermanentDeleteCommand>(validationRuleFactory);
