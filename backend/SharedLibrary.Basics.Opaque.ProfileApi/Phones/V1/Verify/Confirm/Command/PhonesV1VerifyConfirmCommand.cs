namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Command;

public sealed record PhonesV1VerifyConfirmCommand : IHandleableCommand<
    PhonesV1VerifyConfirmCommand,
    PhonesV1VerifyConfirmCommandValidator,
    PhonesV1VerifyConfirmCommandHandler,
    PhonesV1VerifyConfirmCommandResult>
{
    public required Guid SessionId { get; init; }
    public required string Code { get; init; }
}
