namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Command;

public sealed record EmailsV1VerifyConfirmCommand : IHandleableCommand<
    EmailsV1VerifyConfirmCommand,
    EmailsV1VerifyConfirmCommandValidator,
    EmailsV1VerifyConfirmCommandHandler,
    EmailsV1VerifyConfirmCommandResult>
{
    public required Guid SessionId { get; init; }
    public required string Code { get; init; }
}
