namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1UpdateCommandValidator(
    ValidationRuleFactory<EmailsV1UpdateCommand> validationRuleFactory)
    : BaseValidator<EmailsV1UpdateCommand>(validationRuleFactory);
