namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaTotpV1DeleteCommandValidator(
    ValidationRuleFactory<MfaTotpV1DeleteCommand> validationRuleFactory)
    : BaseValidator<MfaTotpV1DeleteCommand>(validationRuleFactory);
