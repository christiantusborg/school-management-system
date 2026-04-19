namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1ChangeRoleCommandValidator(
    ValidationRuleFactory<AdminUsersV1ChangeRoleCommand> validationRuleFactory)
    : BaseValidator<AdminUsersV1ChangeRoleCommand>(validationRuleFactory);
