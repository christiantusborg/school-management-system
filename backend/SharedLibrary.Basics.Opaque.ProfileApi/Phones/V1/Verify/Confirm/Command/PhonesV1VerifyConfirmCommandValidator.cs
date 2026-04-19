namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1VerifyConfirmCommandValidator(
    ValidationRuleFactory<PhonesV1VerifyConfirmCommand> validationRuleFactory)
    : BaseValidator<PhonesV1VerifyConfirmCommand>(validationRuleFactory);
