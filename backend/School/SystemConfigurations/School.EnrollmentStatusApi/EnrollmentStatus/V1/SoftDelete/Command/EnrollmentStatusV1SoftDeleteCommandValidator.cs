namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1SoftDeleteCommandValidator(
    ValidationRuleFactory<EnrollmentStatusV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<EnrollmentStatusV1SoftDeleteCommand>(validationRuleFactory);
