namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class LoginV1FinishCommandValidator(
    ValidationRuleFactory<LoginV1FinishCommand> validationRuleFactory)
    : BaseValidator<LoginV1FinishCommand>(validationRuleFactory);
