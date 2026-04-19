namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PasswordResetV1FinalizeCommandValidator(
    ValidationRuleFactory<PasswordResetV1FinalizeCommand> validationRuleFactory)
    : BaseValidator<PasswordResetV1FinalizeCommand>(validationRuleFactory);
