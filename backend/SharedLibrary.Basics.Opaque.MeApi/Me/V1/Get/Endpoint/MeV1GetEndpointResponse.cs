namespace SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Endpoint;

public sealed class MeV1GetEndpointResponse : HateoasBaseResponse
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    public required string[] Roles { get; init; }
    public required bool IsEnabled { get; init; }
    public required DateTime CreatedAt { get; init; }
}
