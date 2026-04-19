namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Command;

public sealed class PhonesV1DeleteCommand : IHandleableCommand<
    PhonesV1DeleteCommand,
    PhonesV1DeleteCommandValidator,
    PhonesV1DeleteCommandHandler,
    PhonesV1DeleteCommandResult>
{
    public required Guid UserPhoneId { get; init; }
    public required string UserId { get; init; }
}
