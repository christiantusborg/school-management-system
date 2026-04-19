namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1DeleteCommandValidator(
    ValidationRuleFactory<MfaSmsV1DeleteCommand> validationRuleFactory)
    : BaseValidator<MfaSmsV1DeleteCommand>(validationRuleFactory);
