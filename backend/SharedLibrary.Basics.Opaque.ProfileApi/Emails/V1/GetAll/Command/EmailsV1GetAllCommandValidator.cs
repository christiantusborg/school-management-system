namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1GetAllCommandValidator(
    ValidationRuleFactory<EmailsV1GetAllCommand> validationRuleFactory)
    : BaseValidator<EmailsV1GetAllCommand>(validationRuleFactory);
