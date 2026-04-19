namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RecoveryCodesV1GetStatusCommandValidator(
    ValidationRuleFactory<RecoveryCodesV1GetStatusCommand> validationRuleFactory)
    : BaseValidator<RecoveryCodesV1GetStatusCommand>(validationRuleFactory);
