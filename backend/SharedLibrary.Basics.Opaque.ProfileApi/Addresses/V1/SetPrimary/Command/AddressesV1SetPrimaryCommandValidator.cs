namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1SetPrimaryCommandValidator(
    ValidationRuleFactory<AddressesV1SetPrimaryCommand> validationRuleFactory)
    : BaseValidator<AddressesV1SetPrimaryCommand>(validationRuleFactory);
