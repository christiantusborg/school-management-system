namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify;

public class EmailVerifyState
{
    public required Guid UserContactEmailId { get; init; }
    public required string UserId { get; init; }
    public required string Code { get; init; }
}
