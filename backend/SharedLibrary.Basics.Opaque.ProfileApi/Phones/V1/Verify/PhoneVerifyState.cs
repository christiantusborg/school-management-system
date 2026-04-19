namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify;

public class PhoneVerifyState
{
    public required Guid UserPhoneId { get; init; }
    public required string UserId { get; init; }
    public required string Code { get; init; }
}
