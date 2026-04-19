namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class UsersV1SearchCommandValidator(
    ValidationRuleFactory<UsersV1SearchCommand> validationRuleFactory)
    : BaseValidator<UsersV1SearchCommand>(validationRuleFactory);
