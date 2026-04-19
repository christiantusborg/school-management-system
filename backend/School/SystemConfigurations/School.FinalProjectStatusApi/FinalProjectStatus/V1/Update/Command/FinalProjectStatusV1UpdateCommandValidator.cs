namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1UpdateCommandValidator(
    ValidationRuleFactory<FinalProjectStatusV1UpdateCommand> validationRuleFactory)
    : BaseValidator<FinalProjectStatusV1UpdateCommand>(validationRuleFactory);
