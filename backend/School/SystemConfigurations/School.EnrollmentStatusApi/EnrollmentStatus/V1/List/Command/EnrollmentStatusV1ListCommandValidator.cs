namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1ListCommandValidator(
    ValidationRuleFactory<EnrollmentStatusV1ListCommand> validationRuleFactory)
    : BaseValidator<EnrollmentStatusV1ListCommand>(validationRuleFactory);
