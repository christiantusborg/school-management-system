namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1AssertionFinishCommandValidator(
    ValidationRuleFactory<MfaFido2V1AssertionFinishCommand> validationRuleFactory)
    : BaseValidator<MfaFido2V1AssertionFinishCommand>(validationRuleFactory);
