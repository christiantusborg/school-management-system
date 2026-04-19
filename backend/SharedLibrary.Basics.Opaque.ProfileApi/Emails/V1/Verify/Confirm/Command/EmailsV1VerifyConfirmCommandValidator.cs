namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1VerifyConfirmCommandValidator(
    ValidationRuleFactory<EmailsV1VerifyConfirmCommand> validationRuleFactory)
    : BaseValidator<EmailsV1VerifyConfirmCommand>(validationRuleFactory);
