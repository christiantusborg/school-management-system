namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PasswordResetV1InitCommandValidator(
    ValidationRuleFactory<PasswordResetV1InitCommand> validationRuleFactory)
    : BaseValidator<PasswordResetV1InitCommand>(validationRuleFactory);
