namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1VerifyCommandValidator(
    ValidationRuleFactory<MfaEmailV1VerifyCommand> validationRuleFactory)
    : BaseValidator<MfaEmailV1VerifyCommand>(validationRuleFactory);
