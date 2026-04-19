namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Command;

public sealed record EmailsV1VerifyInitCommand : IHandleableCommand<
    EmailsV1VerifyInitCommand,
    EmailsV1VerifyInitCommandValidator,
    EmailsV1VerifyInitCommandHandler,
    EmailsV1VerifyInitCommandResult>
{
    public required string UserId { get; init; }
    public required Guid UserContactEmailId { get; init; }
}
