namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Command;

public sealed class PhonesV1GetAllCommand : IHandleableCommand<
    PhonesV1GetAllCommand,
    PhonesV1GetAllCommandValidator,
    PhonesV1GetAllCommandHandler,
    CommandSearchResult<PhonesV1GetAllCommandResultItem>>
{
    public required string UserId { get; init; }
}
