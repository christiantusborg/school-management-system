namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1GetCommandValidator(
    ValidationRuleFactory<PhonesV1GetCommand> validationRuleFactory)
    : BaseValidator<PhonesV1GetCommand>(validationRuleFactory);
