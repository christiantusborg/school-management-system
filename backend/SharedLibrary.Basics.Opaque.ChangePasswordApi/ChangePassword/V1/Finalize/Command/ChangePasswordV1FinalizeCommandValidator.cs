namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ChangePasswordV1FinalizeCommandValidator(
    ValidationRuleFactory<ChangePasswordV1FinalizeCommand> validationRuleFactory)
    : BaseValidator<ChangePasswordV1FinalizeCommand>(validationRuleFactory);
