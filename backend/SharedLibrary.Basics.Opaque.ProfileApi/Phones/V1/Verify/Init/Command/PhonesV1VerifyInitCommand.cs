namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Command;

public sealed record PhonesV1VerifyInitCommand : IHandleableCommand<
    PhonesV1VerifyInitCommand,
    PhonesV1VerifyInitCommandValidator,
    PhonesV1VerifyInitCommandHandler,
    PhonesV1VerifyInitCommandResult>
{
    public required string UserId { get; init; }
    public required Guid UserPhoneId { get; init; }
}
