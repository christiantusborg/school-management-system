namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<EnrollmentStatusV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<EnrollmentStatusV1PermanentDeleteCommand>(validationRuleFactory);
