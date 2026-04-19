namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1SoftDeleteCommandValidator(
    ValidationRuleFactory<TuitionFeeStatusV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<TuitionFeeStatusV1SoftDeleteCommand>(validationRuleFactory);
