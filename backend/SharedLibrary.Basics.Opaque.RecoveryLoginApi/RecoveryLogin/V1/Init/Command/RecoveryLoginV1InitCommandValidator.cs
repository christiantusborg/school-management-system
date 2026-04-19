namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RecoveryLoginV1InitCommandValidator(
    ValidationRuleFactory<RecoveryLoginV1InitCommand> validationRuleFactory)
    : BaseValidator<RecoveryLoginV1InitCommand>(validationRuleFactory);
