namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1GetAllCommandValidator(
    ValidationRuleFactory<AddressesV1GetAllCommand> validationRuleFactory)
    : BaseValidator<AddressesV1GetAllCommand>(validationRuleFactory);
