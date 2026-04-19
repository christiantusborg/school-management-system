namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1UpdateCommandValidator(
    ValidationRuleFactory<AddressesV1UpdateCommand> validationRuleFactory)
    : BaseValidator<AddressesV1UpdateCommand>(validationRuleFactory);
