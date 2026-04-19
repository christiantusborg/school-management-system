namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminInviteCodesV1CreateCommandValidator(
    ValidationRuleFactory<AdminInviteCodesV1CreateCommand> validationRuleFactory)
    : BaseValidator<AdminInviteCodesV1CreateCommand>(validationRuleFactory);
