namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1EnableConfirmCommandValidator(
    ValidationRuleFactory<MfaSmsV1EnableConfirmCommand> validationRuleFactory)
    : BaseValidator<MfaSmsV1EnableConfirmCommand>(validationRuleFactory);
