namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaTotpV1EnableInitCommandValidator(
    ValidationRuleFactory<MfaTotpV1EnableInitCommand> validationRuleFactory)
    : BaseValidator<MfaTotpV1EnableInitCommand>(validationRuleFactory);
