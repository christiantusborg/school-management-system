namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1GetCommandValidator(
    ValidationRuleFactory<TuitionFeeStatusV1GetCommand> validationRuleFactory)
    : BaseValidator<TuitionFeeStatusV1GetCommand>(validationRuleFactory);
