namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1VerifyCommandValidator(
    ValidationRuleFactory<MfaSmsV1VerifyCommand> validationRuleFactory)
    : BaseValidator<MfaSmsV1VerifyCommand>(validationRuleFactory);
