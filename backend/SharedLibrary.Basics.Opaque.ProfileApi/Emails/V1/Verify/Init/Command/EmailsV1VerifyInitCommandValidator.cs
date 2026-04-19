namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1VerifyInitCommandValidator(
    ValidationRuleFactory<EmailsV1VerifyInitCommand> validationRuleFactory)
    : BaseValidator<EmailsV1VerifyInitCommand>(validationRuleFactory);
