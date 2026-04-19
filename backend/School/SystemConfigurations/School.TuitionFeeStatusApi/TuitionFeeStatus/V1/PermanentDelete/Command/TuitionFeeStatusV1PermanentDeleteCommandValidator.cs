namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<TuitionFeeStatusV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<TuitionFeeStatusV1PermanentDeleteCommand>(validationRuleFactory);
