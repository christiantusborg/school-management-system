namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ChangePasswordV1InitCommandValidator(
    ValidationRuleFactory<ChangePasswordV1InitCommand> validationRuleFactory)
    : BaseValidator<ChangePasswordV1InitCommand>(validationRuleFactory);
