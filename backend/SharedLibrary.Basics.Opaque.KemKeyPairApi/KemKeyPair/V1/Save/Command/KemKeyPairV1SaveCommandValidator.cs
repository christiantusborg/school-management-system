namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class KemKeyPairV1SaveCommandValidator(
    ValidationRuleFactory<KemKeyPairV1SaveCommand> validationRuleFactory)
    : BaseValidator<KemKeyPairV1SaveCommand>(validationRuleFactory);
