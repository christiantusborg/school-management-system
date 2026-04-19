namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Command;

public sealed class UsersV1SearchCommand
    : IHandleableCommand<UsersV1SearchCommand, UsersV1SearchCommandValidator, UsersV1SearchCommandHandler, UsersV1SearchCommandResult>
{
    public required string Query { get; init; }
}
