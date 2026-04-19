namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1DeleteCommandValidator(
    ValidationRuleFactory<MfaEmailV1DeleteCommand> validationRuleFactory)
    : BaseValidator<MfaEmailV1DeleteCommand>(validationRuleFactory);
