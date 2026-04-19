using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Command;

public sealed class PhonesV1GetAllCommandResultItem : IPhonesV1GetAllCommandResultItemQueue
{
    public required Guid UserPhoneId { get; init; }
    public required string Number { get; init; }
    public string? Label { get; init; }
    public required bool IsPrimary { get; init; }
    public required bool IsVerified { get; init; }
}
