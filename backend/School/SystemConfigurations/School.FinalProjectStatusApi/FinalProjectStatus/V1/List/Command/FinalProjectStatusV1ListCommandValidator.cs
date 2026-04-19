namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1ListCommandValidator(
    ValidationRuleFactory<FinalProjectStatusV1ListCommand> validationRuleFactory)
    : BaseValidator<FinalProjectStatusV1ListCommand>(validationRuleFactory);
