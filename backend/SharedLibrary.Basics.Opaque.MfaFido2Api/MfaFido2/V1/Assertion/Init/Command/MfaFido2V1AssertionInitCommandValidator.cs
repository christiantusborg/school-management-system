namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1AssertionInitCommandValidator(
    ValidationRuleFactory<MfaFido2V1AssertionInitCommand> validationRuleFactory)
    : BaseValidator<MfaFido2V1AssertionInitCommand>(validationRuleFactory);
