namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Command;

public sealed record EmailsV1CreateCommand : IHandleableCommand<
    EmailsV1CreateCommand,
    EmailsV1CreateCommandValidator,
    EmailsV1CreateCommandHandler,
    EmailsV1CreateCommandResult>
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public string? Label { get; init; }
}
