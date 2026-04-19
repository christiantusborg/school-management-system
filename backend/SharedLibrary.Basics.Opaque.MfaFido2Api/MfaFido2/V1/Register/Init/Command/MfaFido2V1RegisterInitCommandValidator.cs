namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1RegisterInitCommandValidator(
    ValidationRuleFactory<MfaFido2V1RegisterInitCommand> validationRuleFactory)
    : BaseValidator<MfaFido2V1RegisterInitCommand>(validationRuleFactory);
