namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1ListCommandValidator(
    ValidationRuleFactory<TuitionFeeStatusV1ListCommand> validationRuleFactory)
    : BaseValidator<TuitionFeeStatusV1ListCommand>(validationRuleFactory);
