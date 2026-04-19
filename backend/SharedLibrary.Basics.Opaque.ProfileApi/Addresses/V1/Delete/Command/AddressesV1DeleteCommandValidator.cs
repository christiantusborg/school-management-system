namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1DeleteCommandValidator(
    ValidationRuleFactory<AddressesV1DeleteCommand> validationRuleFactory)
    : BaseValidator<AddressesV1DeleteCommand>(validationRuleFactory);
