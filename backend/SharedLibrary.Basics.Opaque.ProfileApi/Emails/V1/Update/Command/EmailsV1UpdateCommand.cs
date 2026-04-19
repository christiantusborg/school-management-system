namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Command;

public sealed record EmailsV1UpdateCommand : IHandleableCommand<
    EmailsV1UpdateCommand,
    EmailsV1UpdateCommandValidator,
    EmailsV1UpdateCommandHandler,
    EmailsV1UpdateCommandResult>
{
    public required Guid UserContactEmailId { get; init; }
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public string? Label { get; init; }
}
