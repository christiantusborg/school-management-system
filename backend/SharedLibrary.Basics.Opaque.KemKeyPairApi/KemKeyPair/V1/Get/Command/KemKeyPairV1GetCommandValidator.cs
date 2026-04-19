namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class KemKeyPairV1GetCommandValidator(
    ValidationRuleFactory<KemKeyPairV1GetCommand> validationRuleFactory)
    : BaseValidator<KemKeyPairV1GetCommand>(validationRuleFactory);
