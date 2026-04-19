namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1CreateCommandValidator(
    ValidationRuleFactory<AddressesV1CreateCommand> validationRuleFactory)
    : BaseValidator<AddressesV1CreateCommand>(validationRuleFactory);
