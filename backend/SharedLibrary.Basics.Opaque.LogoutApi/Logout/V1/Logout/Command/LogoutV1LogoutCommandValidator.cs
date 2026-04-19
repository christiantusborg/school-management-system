namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class LogoutV1LogoutCommandValidator(
    ValidationRuleFactory<LogoutV1LogoutCommand> validationRuleFactory)
    : BaseValidator<LogoutV1LogoutCommand>(validationRuleFactory);
