namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1SendCommandValidator(
    ValidationRuleFactory<MfaSmsV1SendCommand> validationRuleFactory)
    : BaseValidator<MfaSmsV1SendCommand>(validationRuleFactory);
