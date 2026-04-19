namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Command;

public sealed class EmailsV1SetPrimaryCommand : IHandleableCommand<
    EmailsV1SetPrimaryCommand,
    EmailsV1SetPrimaryCommandValidator,
    EmailsV1SetPrimaryCommandHandler,
    EmailsV1SetPrimaryCommandResult>
{
    public required Guid UserContactEmailId { get; init; }
    public required string UserId { get; init; }
}
