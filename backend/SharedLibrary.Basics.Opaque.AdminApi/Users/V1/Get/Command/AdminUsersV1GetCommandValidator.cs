namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1GetCommandValidator(
    ValidationRuleFactory<AdminUsersV1GetCommand> validationRuleFactory)
    : BaseValidator<AdminUsersV1GetCommand>(validationRuleFactory);
