namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1ListCommandValidator(
    ValidationRuleFactory<AdminUsersV1ListCommand> validationRuleFactory)
    : BaseValidator<AdminUsersV1ListCommand>(validationRuleFactory);
