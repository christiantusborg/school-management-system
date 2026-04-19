namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Endpoint;

public class EmailsV1GetAllEndpointResponseItem : HateoasBaseResponse
{
    public required Guid UserContactEmailId { get; init; }
    public required string Email { get; init; }
    public string? Label { get; init; }
    public required bool IsPrimary { get; init; }
    public required bool IsVerified { get; init; }
}
