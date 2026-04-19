namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class LogoutV1LogoutEverywhereCommandValidator(
    ValidationRuleFactory<LogoutV1LogoutEverywhereCommand> validationRuleFactory)
    : BaseValidator<LogoutV1LogoutEverywhereCommand>(validationRuleFactory);
