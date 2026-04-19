namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaTotpV1VerifyCommandValidator(
    ValidationRuleFactory<MfaTotpV1VerifyCommand> validationRuleFactory)
    : BaseValidator<MfaTotpV1VerifyCommand>(validationRuleFactory);
