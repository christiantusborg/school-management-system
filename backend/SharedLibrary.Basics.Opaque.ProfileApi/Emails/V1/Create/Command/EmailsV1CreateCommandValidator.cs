namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1CreateCommandValidator(
    ValidationRuleFactory<EmailsV1CreateCommand> validationRuleFactory)
    : BaseValidator<EmailsV1CreateCommand>(validationRuleFactory);
