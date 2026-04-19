namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1SetPrimaryCommandValidator(
    ValidationRuleFactory<EmailsV1SetPrimaryCommand> validationRuleFactory)
    : BaseValidator<EmailsV1SetPrimaryCommand>(validationRuleFactory);
