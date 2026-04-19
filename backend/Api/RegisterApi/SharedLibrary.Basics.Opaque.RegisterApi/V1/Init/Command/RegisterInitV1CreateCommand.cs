using System.Security.Claims;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Command;

public sealed class RegisterInitV1CreateCommand : IHandleableCommand<RegisterInitV1CreateCommand, RegisterInitV1CreateCommandValidator,
        RegisterInitV1CreateCommandHandler, RegisterInitV1CreateCommandResult>
{
    public  required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string BlindedElement { get; init; }
    public  string? InviteCode { get; init; }
    public DateTime CreatedAt { get; init; }
    
}