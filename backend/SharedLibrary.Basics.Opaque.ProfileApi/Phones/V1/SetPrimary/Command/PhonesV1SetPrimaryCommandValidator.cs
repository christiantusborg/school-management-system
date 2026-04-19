namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1SetPrimaryCommandValidator(
    ValidationRuleFactory<PhonesV1SetPrimaryCommand> validationRuleFactory)
    : BaseValidator<PhonesV1SetPrimaryCommand>(validationRuleFactory);
