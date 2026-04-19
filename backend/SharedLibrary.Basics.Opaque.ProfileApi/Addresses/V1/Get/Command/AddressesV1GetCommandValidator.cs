namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1GetCommandValidator(
    ValidationRuleFactory<AddressesV1GetCommand> validationRuleFactory)
    : BaseValidator<AddressesV1GetCommand>(validationRuleFactory);
