namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminInviteCodesV1ListCommandValidator(
    ValidationRuleFactory<AdminInviteCodesV1ListCommand> validationRuleFactory)
    : BaseValidator<AdminInviteCodesV1ListCommand>(validationRuleFactory);
