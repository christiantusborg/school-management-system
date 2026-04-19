namespace SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Command;

public sealed class AccountV1DeleteCommand
    : IHandleableCommand<AccountV1DeleteCommand, AccountV1DeleteCommandValidator, AccountV1DeleteCommandHandler, AccountV1DeleteCommandResult>
{
    public required string UserId { get; init; }
}
