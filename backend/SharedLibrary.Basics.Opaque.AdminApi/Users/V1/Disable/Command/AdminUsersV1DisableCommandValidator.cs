namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1DisableCommandValidator(
    ValidationRuleFactory<AdminUsersV1DisableCommand> validationRuleFactory)
    : BaseValidator<AdminUsersV1DisableCommand>(validationRuleFactory);
