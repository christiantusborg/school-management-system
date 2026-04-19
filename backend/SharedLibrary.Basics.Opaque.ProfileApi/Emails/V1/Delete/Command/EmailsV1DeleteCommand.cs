namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Command;

public sealed class EmailsV1DeleteCommand : IHandleableCommand<
    EmailsV1DeleteCommand,
    EmailsV1DeleteCommandValidator,
    EmailsV1DeleteCommandHandler,
    EmailsV1DeleteCommandResult>
{
    public required Guid UserContactEmailId { get; init; }
    public required string UserId { get; init; }
}
