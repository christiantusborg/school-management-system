namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Command;

public sealed class PhonesV1DeleteCommandResult : IPhonesV1DeleteCommandResultQueue
{
    public required Guid UserPhoneId { get; init; }
}
