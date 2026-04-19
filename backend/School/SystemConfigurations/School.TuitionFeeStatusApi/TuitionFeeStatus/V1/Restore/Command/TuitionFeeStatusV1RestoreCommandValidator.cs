namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1RestoreCommandValidator(
    ValidationRuleFactory<TuitionFeeStatusV1RestoreCommand> validationRuleFactory)
    : BaseValidator<TuitionFeeStatusV1RestoreCommand>(validationRuleFactory);
