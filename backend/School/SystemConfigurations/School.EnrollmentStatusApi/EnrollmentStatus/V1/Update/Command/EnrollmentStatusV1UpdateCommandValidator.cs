namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1UpdateCommandValidator(
    ValidationRuleFactory<EnrollmentStatusV1UpdateCommand> validationRuleFactory)
    : BaseValidator<EnrollmentStatusV1UpdateCommand>(validationRuleFactory);
