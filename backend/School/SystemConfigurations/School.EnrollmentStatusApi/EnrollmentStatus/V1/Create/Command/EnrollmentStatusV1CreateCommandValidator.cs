namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1CreateCommandValidator(
    ValidationRuleFactory<EnrollmentStatusV1CreateCommand> validationRuleFactory)
    : BaseValidator<EnrollmentStatusV1CreateCommand>(validationRuleFactory);
