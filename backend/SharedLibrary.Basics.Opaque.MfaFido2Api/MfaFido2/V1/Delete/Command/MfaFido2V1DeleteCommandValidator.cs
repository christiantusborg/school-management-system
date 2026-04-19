namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1DeleteCommandValidator(
    ValidationRuleFactory<MfaFido2V1DeleteCommand> validationRuleFactory)
    : BaseValidator<MfaFido2V1DeleteCommand>(validationRuleFactory);
