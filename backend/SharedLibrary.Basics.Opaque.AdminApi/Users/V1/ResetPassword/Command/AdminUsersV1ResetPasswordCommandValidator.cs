namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1ResetPasswordCommandValidator(
    ValidationRuleFactory<AdminUsersV1ResetPasswordCommand> validationRuleFactory)
    : BaseValidator<AdminUsersV1ResetPasswordCommand>(validationRuleFactory);
