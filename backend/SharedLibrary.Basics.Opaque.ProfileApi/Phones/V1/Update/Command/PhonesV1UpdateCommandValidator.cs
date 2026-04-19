namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1UpdateCommandValidator(
    ValidationRuleFactory<PhonesV1UpdateCommand> validationRuleFactory)
    : BaseValidator<PhonesV1UpdateCommand>(validationRuleFactory);
