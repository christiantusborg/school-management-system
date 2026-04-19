namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1CreateCommandValidator(
    ValidationRuleFactory<PhonesV1CreateCommand> validationRuleFactory)
    : BaseValidator<PhonesV1CreateCommand>(validationRuleFactory);
