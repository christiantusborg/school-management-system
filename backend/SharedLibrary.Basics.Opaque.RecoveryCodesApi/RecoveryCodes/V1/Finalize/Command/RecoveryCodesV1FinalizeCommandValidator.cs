namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RecoveryCodesV1FinalizeCommandValidator(
    ValidationRuleFactory<RecoveryCodesV1FinalizeCommand> validationRuleFactory)
    : BaseValidator<RecoveryCodesV1FinalizeCommand>(validationRuleFactory);
