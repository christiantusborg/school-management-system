namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Command;

/// <inheritdoc />
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RegisterFinalizeV1CreateCommandValidator(
    ValidationRuleFactory<RegisterFinalizeV1CreateCommand> validationRuleFactory)
    : BaseValidator<RegisterFinalizeV1CreateCommand>(validationRuleFactory);
