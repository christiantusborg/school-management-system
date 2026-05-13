namespace School.PartnerAdminApi.Partner.V1.AddUser.Endpoint;

public sealed class AdminPartnerV1AddUserEndpointRequest
{
    public required string Username { get; init; }
    public string? Email { get; init; }
    // Optional custom password. Blank → server generates random.
    public string? Password { get; init; }
}
