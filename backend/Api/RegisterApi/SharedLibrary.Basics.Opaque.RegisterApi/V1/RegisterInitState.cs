namespace SharedLibrary.Basics.Opaque.RegisterApi.V1;

public class RegisterInitState
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required byte[] Seed { get; init; }
    public required string? InviteCodeId { get; init; }
    public required string AssignedRole { get; init; }
}