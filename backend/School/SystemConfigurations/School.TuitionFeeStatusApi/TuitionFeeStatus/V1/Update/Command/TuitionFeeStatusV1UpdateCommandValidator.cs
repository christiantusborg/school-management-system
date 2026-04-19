namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1UpdateCommandValidator(
    ValidationRuleFactory<TuitionFeeStatusV1UpdateCommand> validationRuleFactory)
    : BaseValidator<TuitionFeeStatusV1UpdateCommand>(validationRuleFactory);
