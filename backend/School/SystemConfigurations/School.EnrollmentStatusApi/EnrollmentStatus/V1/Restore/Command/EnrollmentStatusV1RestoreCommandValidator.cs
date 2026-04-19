namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1RestoreCommandValidator(
    ValidationRuleFactory<EnrollmentStatusV1RestoreCommand> validationRuleFactory)
    : BaseValidator<EnrollmentStatusV1RestoreCommand>(validationRuleFactory);
