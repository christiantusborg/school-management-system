namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1VerifyInitCommandValidator(
    ValidationRuleFactory<PhonesV1VerifyInitCommand> validationRuleFactory)
    : BaseValidator<PhonesV1VerifyInitCommand>(validationRuleFactory);
