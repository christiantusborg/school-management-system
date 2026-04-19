namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1DeleteCommandValidator(
    ValidationRuleFactory<EmailsV1DeleteCommand> validationRuleFactory)
    : BaseValidator<EmailsV1DeleteCommand>(validationRuleFactory);
