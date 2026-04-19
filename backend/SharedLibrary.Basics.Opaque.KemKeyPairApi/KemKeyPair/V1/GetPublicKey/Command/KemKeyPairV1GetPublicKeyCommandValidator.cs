namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class KemKeyPairV1GetPublicKeyCommandValidator(
    ValidationRuleFactory<KemKeyPairV1GetPublicKeyCommand> validationRuleFactory)
    : BaseValidator<KemKeyPairV1GetPublicKeyCommand>(validationRuleFactory);
