namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1EnableInitCommandValidator(
    ValidationRuleFactory<MfaSmsV1EnableInitCommand> validationRuleFactory)
    : BaseValidator<MfaSmsV1EnableInitCommand>(validationRuleFactory);
