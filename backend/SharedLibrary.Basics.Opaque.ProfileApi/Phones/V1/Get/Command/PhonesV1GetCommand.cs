namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Command;

public sealed class PhonesV1GetCommand : IHandleableCommand<
    PhonesV1GetCommand,
    PhonesV1GetCommandValidator,
    PhonesV1GetCommandHandler,
    PhonesV1GetCommandResult>
{
    public required Guid UserPhoneId { get; init; }
    public required string UserId { get; init; }
}
