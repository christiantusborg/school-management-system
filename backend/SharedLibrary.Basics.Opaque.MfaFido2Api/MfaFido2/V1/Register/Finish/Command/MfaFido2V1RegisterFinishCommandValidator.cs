namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1RegisterFinishCommandValidator(
    ValidationRuleFactory<MfaFido2V1RegisterFinishCommand> validationRuleFactory)
    : BaseValidator<MfaFido2V1RegisterFinishCommand>(validationRuleFactory);
