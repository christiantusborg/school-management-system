namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Command;

public sealed class PhonesV1SetPrimaryCommand : IHandleableCommand<
    PhonesV1SetPrimaryCommand,
    PhonesV1SetPrimaryCommandValidator,
    PhonesV1SetPrimaryCommandHandler,
    PhonesV1SetPrimaryCommandResult>
{
    public required Guid UserPhoneId { get; init; }
    public required string UserId { get; init; }
}
