namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Endpoint;

public class RegisterInitV1CreateEndpointRequest
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string BlindedElement { get; init; }
    public  string? InviteCode { get; init; }
}