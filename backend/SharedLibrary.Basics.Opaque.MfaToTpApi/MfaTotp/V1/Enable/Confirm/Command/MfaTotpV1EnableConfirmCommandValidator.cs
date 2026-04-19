namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaTotpV1EnableConfirmCommandValidator(
    ValidationRuleFactory<MfaTotpV1EnableConfirmCommand> validationRuleFactory)
    : BaseValidator<MfaTotpV1EnableConfirmCommand>(validationRuleFactory);
