namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RecoveryCodesV1InitCommandValidator(
    ValidationRuleFactory<RecoveryCodesV1InitCommand> validationRuleFactory)
    : BaseValidator<RecoveryCodesV1InitCommand>(validationRuleFactory);
