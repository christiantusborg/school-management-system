namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1GetCommandValidator(
    ValidationRuleFactory<EmailsV1GetCommand> validationRuleFactory)
    : BaseValidator<EmailsV1GetCommand>(validationRuleFactory);
