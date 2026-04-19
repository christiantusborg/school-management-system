namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class LoginV1InitCommandValidator(
    ValidationRuleFactory<LoginV1InitCommand> validationRuleFactory)
    : BaseValidator<LoginV1InitCommand>(validationRuleFactory);
