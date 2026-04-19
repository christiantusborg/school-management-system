namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Command;

public sealed class EmailsV1GetCommand : IHandleableCommand<
    EmailsV1GetCommand,
    EmailsV1GetCommandValidator,
    EmailsV1GetCommandHandler,
    EmailsV1GetCommandResult>
{
    public required Guid UserContactEmailId { get; init; }
    public required string UserId { get; init; }
}
