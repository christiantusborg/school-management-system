namespace SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AccountV1DeleteCommandValidator(
    ValidationRuleFactory<AccountV1DeleteCommand> validationRuleFactory)
    : BaseValidator<AccountV1DeleteCommand>(validationRuleFactory);
