namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1CreateCommandValidator(
    ValidationRuleFactory<TuitionFeeStatusV1CreateCommand> validationRuleFactory)
    : BaseValidator<TuitionFeeStatusV1CreateCommand>(validationRuleFactory);
