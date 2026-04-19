namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1EnableCommandValidator(
    ValidationRuleFactory<AdminUsersV1EnableCommand> validationRuleFactory)
    : BaseValidator<AdminUsersV1EnableCommand>(validationRuleFactory);
