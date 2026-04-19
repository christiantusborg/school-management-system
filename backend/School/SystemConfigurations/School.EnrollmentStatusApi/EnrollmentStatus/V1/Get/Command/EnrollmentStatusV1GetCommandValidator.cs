namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1GetCommandValidator(
    ValidationRuleFactory<EnrollmentStatusV1GetCommand> validationRuleFactory)
    : BaseValidator<EnrollmentStatusV1GetCommand>(validationRuleFactory);
