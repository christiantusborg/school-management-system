namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Command;

/// <inheritdoc />
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RegisterInitV1CreateCommandValidator(
    ValidationRuleFactory<RegisterInitV1CreateCommand> validationRuleFactory)
    : BaseValidator<RegisterInitV1CreateCommand>(validationRuleFactory);