namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1RestoreCommandValidator(
    ValidationRuleFactory<FinalProjectStatusV1RestoreCommand> validationRuleFactory)
    : BaseValidator<FinalProjectStatusV1RestoreCommand>(validationRuleFactory);
