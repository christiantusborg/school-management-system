namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1EnableInitCommandValidator(
    ValidationRuleFactory<MfaEmailV1EnableInitCommand> validationRuleFactory)
    : BaseValidator<MfaEmailV1EnableInitCommand>(validationRuleFactory);
