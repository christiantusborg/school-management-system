namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1EnableConfirmCommandValidator(
    ValidationRuleFactory<MfaEmailV1EnableConfirmCommand> validationRuleFactory)
    : BaseValidator<MfaEmailV1EnableConfirmCommand>(validationRuleFactory);
