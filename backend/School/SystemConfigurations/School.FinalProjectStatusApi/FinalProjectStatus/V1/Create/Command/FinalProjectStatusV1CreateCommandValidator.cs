namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1CreateCommandValidator(
    ValidationRuleFactory<FinalProjectStatusV1CreateCommand> validationRuleFactory)
    : BaseValidator<FinalProjectStatusV1CreateCommand>(validationRuleFactory);
