namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1SendCommandValidator(
    ValidationRuleFactory<MfaEmailV1SendCommand> validationRuleFactory)
    : BaseValidator<MfaEmailV1SendCommand>(validationRuleFactory);
